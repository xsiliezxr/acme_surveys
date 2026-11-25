using System;
using System.Globalization;
using AcmeSurveys.Domain.Surveys;

namespace AcmeSurveys.Web.Common
{
    public static class ValueFormatter
    {
        private static readonly TimeZoneInfo Guatemala = FindGuatemalaTimeZone();

        public static string Format(object value, CultureInfo culture)
        {
            if (value == null) return string.Empty;
            if (value is decimal number) return number.ToString("#,##0.######", culture);
            if (value is DateTime date) return date.ToString("dd/MM/yyyy", culture);
            return Convert.ToString(value, culture);
        }

        public static string FormatUtc(DateTime utc, CultureInfo culture) =>
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Guatemala).ToString("dd/MM/yyyy HH:mm", culture);

        public static string DisplayName(FieldType type)
        {
            switch (type)
            {
                case FieldType.Text: return "Texto";
                case FieldType.Number: return "Número";
                case FieldType.Date: return "Fecha";
                default: return type.ToString();
            }
        }

        public static string HtmlInputType(FieldType type)
        {
            switch (type)
            {
                case FieldType.Number: return "number";
                case FieldType.Date: return "date";
                default: return "text";
            }
        }

        private static TimeZoneInfo FindGuatemalaTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Central America Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.Utc;
            }
        }
    }
}
