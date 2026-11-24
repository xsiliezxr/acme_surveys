namespace AcmeSurveys.Domain.Responses
{
    public sealed class Answer
    {
        public Answer(int fieldId, string canonicalValue)
        {
            FieldId = fieldId;
            CanonicalValue = canonicalValue;
        }

        public int FieldId { get; }
        public string CanonicalValue { get; }
    }
}
