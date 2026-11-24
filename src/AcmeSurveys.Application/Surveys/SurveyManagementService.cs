using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AcmeSurveys.Application.Abstractions;
using AcmeSurveys.Application.Abstractions.Persistence;
using AcmeSurveys.Domain.Common;
using AcmeSurveys.Domain.Surveys;

namespace AcmeSurveys.Application.Surveys
{
    public sealed class SurveyManagementService
    {
        private const string NotFoundMessage = "La encuesta no existe.";

        private readonly ISurveyRepository _surveys;
        private readonly IIdentifierGenerator _ids;
        private readonly IClock _clock;
        private readonly ISecurityAuditLog _audit;

        public SurveyManagementService(ISurveyRepository surveys, IIdentifierGenerator ids, IClock clock, ISecurityAuditLog audit)
        {
            _surveys = surveys;
            _ids = ids;
            _clock = clock;
            _audit = audit;
        }

        public Task<IReadOnlyList<SurveySummary>> ListAsync(int ownerUserId) => _surveys.ListByOwnerAsync(ownerUserId);

        public async Task<Result<SurveyDetailsDto>> GetAsync(int ownerUserId, Guid externalId)
        {
            var survey = await LoadOwnedAsync(ownerUserId, externalId).ConfigureAwait(false);
            if (survey == null) return Result.NotFound<SurveyDetailsDto>(NotFoundMessage);

            var hasResponses = await _surveys.HasResponsesAsync(survey.Id).ConfigureAwait(false);
            return Result.Success(ToDetails(survey, hasResponses));
        }

        public async Task<Result<Guid>> CreateAsync(int ownerUserId, SaveSurveyCommand command)
        {
            var created = Survey.Create(ownerUserId, _ids.NewRandomUuid(), _ids.NewRandomUuid(),
                command.Name, command.Description, command.Fields, _clock.UtcNow);
            if (created.IsFailure) return Result.Failure<Guid>(created.Errors);

            await _surveys.AddAsync(created.Value).ConfigureAwait(false);
            _audit.Write(SecurityEvent.SurveyCreated, "user:" + ownerUserId, "survey=" + created.Value.ExternalId);
            return Result.Success(created.Value.ExternalId);
        }

        public async Task<Result> UpdateAsync(int ownerUserId, Guid externalId, SaveSurveyCommand command)
        {
            var survey = await LoadOwnedAsync(ownerUserId, externalId).ConfigureAwait(false);
            if (survey == null) return Result.NotFound(NotFoundMessage);

            if (command.RowVersion == null || survey.RowVersion == null ||
                !survey.RowVersion.SequenceEqual(command.RowVersion))
            {
                return ConcurrencyConflict();
            }

            var hasResponses = await _surveys.HasResponsesAsync(survey.Id).ConfigureAwait(false);
            var updated = survey.Update(command.Name, command.Description, command.Fields, hasResponses, _clock.UtcNow);
            if (updated.IsFailure) return updated;

            var outcome = await _surveys.UpdateAsync(survey).ConfigureAwait(false);
            switch (outcome)
            {
                case SaveOutcome.ConcurrencyConflict:
                    return ConcurrencyConflict();
                case SaveOutcome.StructureLockedByResponses:
                    return Result.Failure(new Error(ErrorCodes.Conflict,
                        "Llegaron respuestas mientras editaba: ya no se puede cambiar el tipo ni eliminar campos. Recargue la página."));
                default:
                    _audit.Write(SecurityEvent.SurveyUpdated, "user:" + ownerUserId, "survey=" + externalId);
                    return Result.Success();
            }
        }

        public async Task<Result> DeleteAsync(int ownerUserId, Guid externalId)
        {
            var survey = await LoadOwnedAsync(ownerUserId, externalId).ConfigureAwait(false);
            if (survey == null) return Result.NotFound(NotFoundMessage);

            await _surveys.SoftDeleteAsync(survey.Id, _clock.UtcNow).ConfigureAwait(false);
            _audit.Write(SecurityEvent.SurveyDeleted, "user:" + ownerUserId, "survey=" + externalId);
            return Result.Success();
        }

        public async Task<Result> RotatePublicLinkAsync(int ownerUserId, Guid externalId)
        {
            var survey = await LoadOwnedAsync(ownerUserId, externalId).ConfigureAwait(false);
            if (survey == null) return Result.NotFound(NotFoundMessage);

            survey.RotatePublicToken(_ids.NewRandomUuid(), _clock.UtcNow);
            var outcome = await _surveys.UpdatePublicTokenAsync(survey).ConfigureAwait(false);
            if (outcome != SaveOutcome.Saved) return ConcurrencyConflict();

            _audit.Write(SecurityEvent.PublicLinkRotated, "user:" + ownerUserId, "survey=" + externalId);
            return Result.Success();
        }

        private async Task<Survey> LoadOwnedAsync(int ownerUserId, Guid externalId)
        {
            var survey = await _surveys.FindByExternalIdAsync(externalId).ConfigureAwait(false);
            if (survey == null) return null;

            if (!survey.IsOwnedBy(ownerUserId))
            {
                _audit.Write(SecurityEvent.AccessDenied, "user:" + ownerUserId, "survey=" + externalId + " (not owner)");
                return null;
            }
            return survey;
        }

        private static Result ConcurrencyConflict() =>
            Result.Failure(new Error(ErrorCodes.Conflict,
                "La encuesta fue modificada en otra pestaña o por otra sesión. Recargue la página para ver los cambios."));

        internal static SurveyDetailsDto ToDetails(Survey survey, bool hasResponses) => new SurveyDetailsDto
        {
            ExternalId = survey.ExternalId,
            PublicToken = survey.PublicToken,
            Name = survey.Name,
            Description = survey.Description,
            RowVersion = survey.RowVersion,
            HasResponses = hasResponses,
            CreatedAtUtc = survey.CreatedAtUtc,
            UpdatedAtUtc = survey.UpdatedAtUtc,
            Fields = survey.Fields.Select(f => new SurveyFieldDto
            {
                Id = f.Id,
                Name = f.Name,
                Title = f.Title,
                Type = f.Type,
                IsRequired = f.IsRequired
            }).ToList()
        };
    }
}
