using System;

namespace AcmeSurveys.Application.Abstractions.Persistence
{
    public sealed class ResponseHeader
    {
        public long ResponseId { get; set; }
        public DateTime SubmittedAtUtc { get; set; }
    }
}
