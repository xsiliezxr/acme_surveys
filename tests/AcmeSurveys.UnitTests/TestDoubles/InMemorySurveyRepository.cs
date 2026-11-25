using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AcmeSurveys.Application.Abstractions.Persistence;
using AcmeSurveys.Domain.Surveys;

namespace AcmeSurveys.UnitTests.TestDoubles
{
    internal sealed class InMemorySurveyRepository : ISurveyRepository
    {
        public List<Survey> Surveys { get; } = new List<Survey>();

        public Task<IReadOnlyList<SurveySummary>> ListByOwnerAsync(int ownerUserId) =>
            Task.FromResult<IReadOnlyList<SurveySummary>>(Surveys.Where(s => s.OwnerUserId == ownerUserId)
                .Select(s => new SurveySummary { ExternalId = s.ExternalId, Name = s.Name }).ToList());

        public Task<Survey> FindByExternalIdAsync(Guid externalId) =>
            Task.FromResult(Surveys.FirstOrDefault(s => s.ExternalId == externalId));

        public Task<Survey> FindByPublicTokenAsync(Guid publicToken) =>
            Task.FromResult(Surveys.FirstOrDefault(s => s.PublicToken == publicToken));

        public Task<bool> HasResponsesAsync(int surveyId) => Task.FromResult(false);

        public Task AddAsync(Survey survey)
        {
            Surveys.Add(survey);
            return Task.CompletedTask;
        }

        public Task<SaveOutcome> UpdateAsync(Survey survey) => Task.FromResult(SaveOutcome.Saved);

        public Task<bool> SoftDeleteAsync(int surveyId, DateTime nowUtc) => Task.FromResult(true);

        public Task<SaveOutcome> UpdatePublicTokenAsync(Survey survey) => Task.FromResult(SaveOutcome.Saved);
    }
}
