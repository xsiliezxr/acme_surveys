using System.Collections.Generic;

namespace AcmeSurveys.Application.Abstractions.Persistence
{
    public sealed class EncryptedResultsPage
    {
        public EncryptedResultsPage(long totalCount, IReadOnlyList<ResponseHeader> responses, IReadOnlyList<EncryptedValueRow> values)
        {
            TotalCount = totalCount;
            Responses = responses;
            Values = values;
        }

        public long TotalCount { get; }
        public IReadOnlyList<ResponseHeader> Responses { get; }
        public IReadOnlyList<EncryptedValueRow> Values { get; }
    }
}
