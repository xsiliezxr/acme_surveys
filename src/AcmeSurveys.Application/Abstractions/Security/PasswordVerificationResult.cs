namespace AcmeSurveys.Application.Abstractions.Security
{
    public enum PasswordVerificationResult
    {
        Failed,
        Success,

        SuccessRehashNeeded
    }
}
