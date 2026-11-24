using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AcmeSurveys.Application.Abstractions.Persistence
{
    public interface IResponseRepository
    {
        Task<SaveOutcome> AddAsync(int surveyId, DateTime submittedAtUtc, IReadOnlyList<EncryptedAnswer> answers);

        Task<EncryptedResultsPage> GetPageAsync(int surveyId, int skip, int take);
    }
}
