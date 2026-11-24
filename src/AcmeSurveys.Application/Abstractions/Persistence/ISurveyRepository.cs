using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AcmeSurveys.Domain.Surveys;

namespace AcmeSurveys.Application.Abstractions.Persistence
{
    public interface ISurveyRepository
    {
        Task<IReadOnlyList<SurveySummary>> ListByOwnerAsync(int ownerUserId);

        Task<Survey> FindByExternalIdAsync(Guid externalId);

        Task<Survey> FindByPublicTokenAsync(Guid publicToken);

        Task<bool> HasResponsesAsync(int surveyId);

        Task AddAsync(Survey survey);

        Task<SaveOutcome> UpdateAsync(Survey survey);

        Task<bool> SoftDeleteAsync(int surveyId, DateTime nowUtc);

        Task<SaveOutcome> UpdatePublicTokenAsync(Survey survey);
    }
}
