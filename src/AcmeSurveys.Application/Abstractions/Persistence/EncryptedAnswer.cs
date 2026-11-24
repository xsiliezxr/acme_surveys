namespace AcmeSurveys.Application.Abstractions.Persistence
{
    public sealed class EncryptedAnswer
    {
        public EncryptedAnswer(int fieldId, byte[] valueCipher)
        {
            FieldId = fieldId;
            ValueCipher = valueCipher;
        }

        public int FieldId { get; }
        public byte[] ValueCipher { get; }
    }
}
