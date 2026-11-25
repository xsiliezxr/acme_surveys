using System.Globalization;

namespace AcmeSurveys.Web.Models
{
    public sealed class FieldRowViewModel
    {
        public const string TemplateIndex = "__index__";

        public FieldRowViewModel(string index, FieldFormViewModel field, bool surveyHasResponses)
        {
            Index = index;
            Field = field ?? new FieldFormViewModel();

            IsStructureLocked = surveyHasResponses && Field.Id.HasValue;
        }

        public string Index { get; }
        public FieldFormViewModel Field { get; }
        public bool IsStructureLocked { get; }

        public string NameFor(string property) => "Fields[" + Index + "]." + property;

        public string IdFor(string property) => "Fields_" + Index + "__" + property;

        public string IdValue => Field.Id.HasValue ? Field.Id.Value.ToString(CultureInfo.InvariantCulture) : string.Empty;
    }
}
