using AcmeSurveys.Domain.Surveys;

namespace AcmeSurveys.Web.Models
{
    public sealed class FieldFormViewModel
    {
        public int? Id { get; set; }

        public string Name { get; set; }
        public string Title { get; set; }
        public FieldType Type { get; set; }
        public bool IsRequired { get; set; }
    }
}
