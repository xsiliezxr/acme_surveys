using System;
using System.Threading.Tasks;
using AcmeSurveys.Domain.Users;

namespace AcmeSurveys.Application.Abstractions.Persistence
{
    public interface IUserRepository
    {
        Task<User> FindByUsernameAsync(string username);

        Task<User> FindByExternalIdAsync(Guid externalId);

        Task<bool> RegisterFailedLoginAsync(int userId, int maxFailedAttempts, DateTime lockoutEndUtc);

        Task RegisterSuccessfulLoginAsync(int userId, DateTime nowUtc);

        Task UpdatePasswordHashAsync(int userId, PasswordHash passwordHash);
    }
}
