using AcmeSurveys.Domain.Surveys;

namespace AcmeSurveys.Domain.FieldValues
{
    public interface IFieldValueHandler
    {
        FieldType Type { get; }

        bool TryNormalize(string rawValue, out string canonicalValue, out string errorMessage);

        object ToTypedValue(string canonicalValue);
    }
}
