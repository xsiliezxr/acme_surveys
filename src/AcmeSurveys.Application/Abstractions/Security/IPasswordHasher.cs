using AcmeSurveys.Domain.Users;

namespace AcmeSurveys.Application.Abstractions.Security
{
    public interface IPasswordHasher
    {
        PasswordHash Hash(string password);

        PasswordVerificationResult Verify(string password, PasswordHash stored);

        void SimulateVerification(string password);
    }
}
