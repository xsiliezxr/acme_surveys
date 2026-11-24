using System.Data.SqlClient;

namespace AcmeSurveys.Infrastructure.Persistence
{
    internal static class SqlErrors
    {
        public const int ConstraintViolation = 547;

        public const int SurveyStructureLocked = 50001;
        public const int ConcurrencyConflict = 50002;

        public static bool Is(SqlException exception, int number)
        {
            foreach (SqlError error in exception.Errors)
            {
                if (error.Number == number) return true;
            }
            return false;
        }
    }
}
