using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AcmeSurveys.Application.Abstractions;
using AcmeSurveys.Application.Abstractions.Persistence;
using AcmeSurveys.Application.Abstractions.Security;
using AcmeSurveys.Domain.Common;
using AcmeSurveys.Domain.FieldValues;
using AcmeSurveys.Domain.Responses;
using AcmeSurveys.Domain.Surveys;

namespace AcmeSurveys.Application.Responses
{
    public sealed class PublicSurveyService
    {
        private const string UnavailableMessage = "La encuesta no existe o ya no está disponible.";

        private readonly ISurveyRepository _surveys;
        private readonly IResponseRepository _responses;
        private readonly IFieldValueProtector _protector;
        private readonly FieldValueHandlerRegistry _handlers;
        private readonly IClock _clock;

        public PublicSurveyService(ISurveyRepository surveys, IResponseRepository responses,
            IFieldValueProtector protector, FieldValueHandlerRegistry handlers, IClock clock)
        {
            _surveys = surveys;
            _responses = responses;
            _protector = protector;
            _handlers = handlers;
            _clock = clock;
        }

        public async Task<Result<PublicSurveyFormDto>> GetFormAsync(Guid publicToken)
        {
            var survey = await _surveys.FindByPublicTokenAsync(publicToken).ConfigureAwait(false);
            if (survey == null) return Result.NotFound<PublicSurveyFormDto>(UnavailableMessage);
            return Result.Success(ToForm(survey));
        }

        public async Task<Result> SubmitAsync(Guid publicToken, IReadOnlyDictionary<string, string> rawValuesByFieldName)
        {
            var survey = await _surveys.FindByPublicTokenAsync(publicToken).ConfigureAwait(false);
            if (survey == null) return Result.NotFound(UnavailableMessage);

            var response = SurveyResponse.Create(survey, rawValuesByFieldName, _handlers, _clock.UtcNow);
            if (response.IsFailure) return response;

            var encrypted = response.Value.Answers
                .Select(a => new EncryptedAnswer(a.FieldId, _protector.Protect(a.CanonicalValue, a.FieldId)))
                .ToList();

            var outcome = await _responses.AddAsync(survey.Id, response.Value.SubmittedAtUtc, encrypted).ConfigureAwait(false);
            if (outcome == SaveOutcome.SurveyStructureChanged)
            {
                return Result.Failure(new Error(ErrorCodes.Conflict,
                    "La encuesta cambió mientras la llenaba. Recargue la página e intente de nuevo."));
            }

            return Result.Success();
        }

        private static PublicSurveyFormDto ToForm(Survey survey) => new PublicSurveyFormDto
        {
            PublicToken = survey.PublicToken,
            Name = survey.Name,
            Description = survey.Description,
            Fields = survey.Fields.Select(f => new PublicFieldDto
            {
                Name = f.Name,
                Title = f.Title,
                Type = f.Type,
                IsRequired = f.IsRequired
            }).ToList()
        };
    }
}
