using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AcmeSurveys.Application.Abstractions.Persistence;

namespace AcmeSurveys.UnitTests.TestDoubles
{
    internal sealed class InMemoryResponseRepository : IResponseRepository
    {
        public List<IReadOnlyList<EncryptedAnswer>> Saved { get; } = new List<IReadOnlyList<EncryptedAnswer>>();

        public SaveOutcome NextOutcome { get; set; } = SaveOutcome.Saved;

        public Task<SaveOutcome> AddAsync(int surveyId, DateTime submittedAtUtc, IReadOnlyList<EncryptedAnswer> answers)
        {
            if (NextOutcome == SaveOutcome.Saved) Saved.Add(answers);
            return Task.FromResult(NextOutcome);
        }

        public Task<EncryptedResultsPage> GetPageAsync(int surveyId, int skip, int take) =>
            throw new NotSupportedException();
    }
}
