using System;
using System.Globalization;
using AcmeSurveys.Domain.Surveys;

namespace AcmeSurveys.Domain.FieldValues
{
    public sealed class DateFieldHandler : IFieldValueHandler
    {
        public const string CanonicalFormat = "yyyy-MM-dd";
        public static readonly DateTime MinDate = new DateTime(1900, 1, 1);
        public static readonly DateTime MaxDate = new DateTime(2100, 12, 31);

        public FieldType Type => FieldType.Date;

        public bool TryNormalize(string rawValue, out string canonicalValue, out string errorMessage)
        {
            canonicalValue = null;
            errorMessage = null;

            DateTime date;
            if (!DateTime.TryParseExact((rawValue ?? string.Empty).Trim(), CanonicalFormat,
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            {
                errorMessage = "Ingrese una fecha válida.";
                return false;
            }

            if (date < MinDate || date > MaxDate)
            {
                errorMessage = $"La fecha debe estar entre {MinDate:yyyy} y {MaxDate:yyyy}.";
                return false;
            }

            canonicalValue = date.ToString(CanonicalFormat, CultureInfo.InvariantCulture);
            return true;
        }

        public object ToTypedValue(string canonicalValue) =>
            DateTime.ParseExact(canonicalValue, CanonicalFormat, CultureInfo.InvariantCulture, DateTimeStyles.None);
    }
}
