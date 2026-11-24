using System;
using System.Collections.Generic;

namespace AcmeSurveys.Application.Responses
{
    public sealed class PublicSurveyFormDto
    {
        public Guid PublicToken { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public IReadOnlyList<PublicFieldDto> Fields { get; set; }
    }
}
