using System;
using System.Collections.Generic;
using AcmeSurveys.Domain.Common;
using AcmeSurveys.Domain.FieldValues;
using AcmeSurveys.Domain.Surveys;

namespace AcmeSurveys.Domain.Responses
{
    public sealed class SurveyResponse
    {
        private SurveyResponse(int surveyId, DateTime submittedAtUtc, IReadOnlyList<Answer> answers)
        {
            SurveyId = surveyId;
            SubmittedAtUtc = submittedAtUtc;
            Answers = answers;
        }

        public int SurveyId { get; }
        public DateTime SubmittedAtUtc { get; }
        public IReadOnlyList<Answer> Answers { get; }

        public static Result<SurveyResponse> Create(
            Survey survey, IReadOnlyDictionary<string, string> rawValuesByFieldName,
            FieldValueHandlerRegistry handlers, DateTime nowUtc)
        {
            if (survey == null) throw new ArgumentNullException(nameof(survey));
            if (handlers == null) throw new ArgumentNullException(nameof(handlers));
            rawValuesByFieldName = rawValuesByFieldName ?? new Dictionary<string, string>();

            var errors = new List<Error>();
            var answers = new List<Answer>(survey.Fields.Count);

            foreach (var field in survey.Fields)
            {
                string raw;
                rawValuesByFieldName.TryGetValue(field.Name, out raw);

                if (string.IsNullOrWhiteSpace(raw))
                {
                    if (field.IsRequired) errors.Add(Error.Validation(field.Name, "Este campo es obligatorio."));
                    continue;
                }

                string canonical, message;
                if (handlers.Get(field.Type).TryNormalize(raw, out canonical, out message))
                    answers.Add(new Answer(field.Id, canonical));
                else
                    errors.Add(Error.Validation(field.Name, message));
            }

            if (errors.Count == 0 && answers.Count == 0)
                errors.Add(Error.Validation(string.Empty, "Responda al menos una pregunta."));

            return errors.Count > 0
                ? Result.Failure<SurveyResponse>(errors)
                : Result.Success(new SurveyResponse(survey.Id, nowUtc, answers));
        }
    }
}
