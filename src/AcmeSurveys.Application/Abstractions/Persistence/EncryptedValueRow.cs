namespace AcmeSurveys.Application.Abstractions.Persistence
{
    public sealed class EncryptedValueRow
    {
        public long ResponseId { get; set; }
        public int SurveyFieldId { get; set; }
        public byte[] ValueCipher { get; set; }
    }
}
