using AcmeSurveys.Domain.Surveys;

namespace AcmeSurveys.Web.Api
{
    /// <summary>Contrato público estable de tipos: "text" | "number" | "date", independiente del enum de C#.</summary>
    internal static class FieldTypes
    {
        public static string ToApi(FieldType type) => type.ToString().ToLowerInvariant();

        public static bool TryParse(string value, out FieldType type)
        {
            switch ((value ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "text": type = FieldType.Text; return true;
                case "number": type = FieldType.Number; return true;
                case "date": type = FieldType.Date; return true;
                default: type = default(FieldType); return false;
            }
        }
    }
}
