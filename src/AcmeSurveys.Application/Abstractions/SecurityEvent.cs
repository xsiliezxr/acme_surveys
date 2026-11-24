namespace AcmeSurveys.Application.Abstractions
{
    public enum SecurityEvent
    {
        LoginSucceeded,
        LoginFailed,
        LoginRejectedLockedOut,
        AccountLockedOut,
        AccessTokenIssued,
        SurveyCreated,
        SurveyUpdated,
        SurveyDeleted,
        PublicLinkRotated,
        AccessDenied,
        AntiForgeryValidationFailed,
        CiphertextIntegrityFailure
    }
}
