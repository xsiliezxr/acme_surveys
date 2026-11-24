namespace AcmeSurveys.Application.Abstractions.Security
{
    public interface IFieldValueProtector
    {
        byte[] Protect(string plaintext, int fieldId);

        string Unprotect(byte[] protectedValue, int fieldId);
    }
}
