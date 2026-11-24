namespace AcmeSurveys.Application.Abstractions.Persistence
{
    public enum SaveOutcome
    {
        Saved,

        ConcurrencyConflict,

        StructureLockedByResponses,

        SurveyStructureChanged
    }
}
