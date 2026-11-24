using AcmeSurveys.Domain.Surveys;

namespace AcmeSurveys.Application.Results
{
    public sealed class ResultColumnDto
    {
        public string Name { get; set; }
        public string Title { get; set; }
        public FieldType Type { get; set; }
    }
}
