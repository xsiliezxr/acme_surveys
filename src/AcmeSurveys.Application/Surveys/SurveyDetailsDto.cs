using System;
using System.Collections.Generic;

namespace AcmeSurveys.Application.Surveys
{
    public sealed class SurveyDetailsDto
    {
        public Guid ExternalId { get; set; }
        public Guid PublicToken { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public byte[] RowVersion { get; set; }
        public bool HasResponses { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
        public IReadOnlyList<SurveyFieldDto> Fields { get; set; }
    }
}
