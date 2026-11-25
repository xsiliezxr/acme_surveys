using AcmeSurveys.Domain.Surveys;

namespace AcmeSurveys.Domain.FieldValues
{
    public sealed class TextFieldHandler : IFieldValueHandler
    {
        public const int MaxLength = 1000;

        public FieldType Type => FieldType.Text;

        public bool TryNormalize(string rawValue, out string canonicalValue, out string errorMessage)
        {
            canonicalValue = null;
            errorMessage = null;

            var value = (rawValue ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Trim();

            if (value.Length > MaxLength)
            {
                errorMessage = $"Máximo {MaxLength} caracteres.";
                return false;
            }

            foreach (var c in value)
            {
                if (char.IsControl(c) && c != '\n' && c != '\t')
                {
                    errorMessage = "El texto contiene caracteres no permitidos.";
                    return false;
                }
            }

            canonicalValue = value;
            return true;
        }

        public object ToTypedValue(string canonicalValue) => canonicalValue;
    }
}
