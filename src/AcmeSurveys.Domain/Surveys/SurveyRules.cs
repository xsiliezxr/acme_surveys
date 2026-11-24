using System.Text.RegularExpressions;

namespace AcmeSurveys.Domain.Surveys
{
    public static class SurveyRules
    {
        public const int NameMinLength = 3;
        public const int NameMaxLength = 150;
        public const int DescriptionMaxLength = 1000;

        public const int MinFields = 1;
        public const int MaxFields = 50;

        public const int FieldNameMaxLength = 50;
        public const int FieldTitleMaxLength = 200;

        public const string FieldNamePattern = "^[A-Za-z][A-Za-z0-9_]{0,49}$";

        public static readonly Regex FieldNameRegex =
            new Regex(FieldNamePattern, RegexOptions.Compiled | RegexOptions.CultureInvariant);
    }
}
