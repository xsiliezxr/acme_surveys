using AcmeSurveys.Domain.Surveys;

namespace AcmeSurveys.Application.Responses
{
    public sealed class PublicFieldDto
    {
        public string Name { get; set; }
        public string Title { get; set; }
        public FieldType Type { get; set; }
        public bool IsRequired { get; set; }
    }
}
