using System;

namespace AcmeSurveys.Application.Abstractions.Persistence
{
    public sealed class SurveySummary
    {
        public Guid ExternalId { get; set; }
        public Guid PublicToken { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int FieldCount { get; set; }
        public long ResponseCount { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
    }
}
