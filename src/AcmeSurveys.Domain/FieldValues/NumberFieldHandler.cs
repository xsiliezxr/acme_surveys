using System;
using System.Globalization;
using AcmeSurveys.Domain.Surveys;

namespace AcmeSurveys.Domain.FieldValues
{
    public sealed class NumberFieldHandler : IFieldValueHandler
    {
        public const int MaxDecimals = 6;
        public const decimal MaxAbsoluteValue = 999999999999999m;

        private const NumberStyles AllowedStyles =
            NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite |
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;

        public FieldType Type => FieldType.Number;

        public bool TryNormalize(string rawValue, out string canonicalValue, out string errorMessage)
        {
            canonicalValue = null;
            errorMessage = null;

            decimal number;
            if (!decimal.TryParse(rawValue, AllowedStyles, CultureInfo.InvariantCulture, out number))
            {
                errorMessage = "Ingrese un número válido (use punto como separador decimal).";
                return false;
            }

            if (Math.Abs(number) > MaxAbsoluteValue)
            {
                errorMessage = "El número está fuera del rango permitido.";
                return false;
            }

            if (GetScale(number) > MaxDecimals)
            {
                errorMessage = $"Máximo {MaxDecimals} decimales.";
                return false;
            }

            canonicalValue = number.ToString("0.######", CultureInfo.InvariantCulture);
            return true;
        }

        public object ToTypedValue(string canonicalValue) =>
            decimal.Parse(canonicalValue, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);

        private static int GetScale(decimal value)
        {
            var normalized = value / 1.000000000000000000000000000000000m;
            return (decimal.GetBits(normalized)[3] >> 16) & 0xFF;
        }
    }
}
