using System;
using System.Threading.Tasks;
using AcmeSurveys.Application.Abstractions;
using AcmeSurveys.Application.Abstractions.Persistence;
using AcmeSurveys.Application.Abstractions.Security;
using AcmeSurveys.Domain.Common;
using AcmeSurveys.Domain.Users;

namespace AcmeSurveys.Application.Auth
{
    public sealed class AuthenticationService
    {
        public const string GenericFailureMessage = "Usuario o contraseña incorrectos.";
        public const int MaxUsernameLength = 50;

        public const int MaxPasswordLength = 128;

        private readonly IUserRepository _users;
        private readonly IPasswordHasher _hasher;
        private readonly IClock _clock;
        private readonly ISecurityAuditLog _audit;
        private readonly LockoutPolicy _lockoutPolicy;

        public AuthenticationService(IUserRepository users, IPasswordHasher hasher, IClock clock,
            ISecurityAuditLog audit, LockoutPolicy lockoutPolicy)
        {
            _users = users;
            _hasher = hasher;
            _clock = clock;
            _audit = audit;
            _lockoutPolicy = lockoutPolicy;
        }

        public async Task<Result<AuthenticatedUser>> AuthenticateAsync(string username, string password, string clientIp)
        {
            var normalizedUsername = (username ?? string.Empty).Trim();
            if (normalizedUsername.Length == 0 || normalizedUsername.Length > MaxUsernameLength ||
                string.IsNullOrEmpty(password) || password.Length > MaxPasswordLength)
            {
                _audit.Write(SecurityEvent.LoginFailed, Truncate(normalizedUsername), "invalid input; ip=" + clientIp);
                return Failure();
            }

            var now = _clock.UtcNow;
            var user = await _users.FindByUsernameAsync(normalizedUsername).ConfigureAwait(false);

            // Usuario inexistente, contraseña errónea y cuenta bloqueada devuelven el mismo mensaje genérico.
            if (user == null)
            {
                _hasher.SimulateVerification(password);
                _audit.Write(SecurityEvent.LoginFailed, Truncate(normalizedUsername), "unknown user; ip=" + clientIp);
                return Failure();
            }

            if (user.IsLockedOut(now))
            {
                _hasher.SimulateVerification(password);
                _audit.Write(SecurityEvent.LoginRejectedLockedOut, user.Username, "ip=" + clientIp);
                return Failure();
            }

            var verification = _hasher.Verify(password, user.Password);
            if (verification == PasswordVerificationResult.Failed)
            {
                var locked = await _users.RegisterFailedLoginAsync(
                    user.Id, _lockoutPolicy.MaxFailedAttempts, now.Add(_lockoutPolicy.LockoutDuration)).ConfigureAwait(false);

                _audit.Write(locked ? SecurityEvent.AccountLockedOut : SecurityEvent.LoginFailed,
                    user.Username, "wrong password; ip=" + clientIp);
                return Failure();
            }

            if (verification == PasswordVerificationResult.SuccessRehashNeeded)
            {
                await _users.UpdatePasswordHashAsync(user.Id, _hasher.Hash(password)).ConfigureAwait(false);
            }

            await _users.RegisterSuccessfulLoginAsync(user.Id, now).ConfigureAwait(false);
            _audit.Write(SecurityEvent.LoginSucceeded, user.Username, "ip=" + clientIp);

            return Result.Success(new AuthenticatedUser(user.Id, user.ExternalId, user.Username));
        }

        public async Task<AuthenticatedUser> FindActiveUserAsync(Guid externalId)
        {
            var user = await _users.FindByExternalIdAsync(externalId).ConfigureAwait(false);
            if (user == null || user.IsLockedOut(_clock.UtcNow)) return null;
            return new AuthenticatedUser(user.Id, user.ExternalId, user.Username);
        }

        private static Result<AuthenticatedUser> Failure() =>
            Result.Failure<AuthenticatedUser>(new Error(ErrorCodes.InvalidCredentials, GenericFailureMessage));

        private static string Truncate(string value) =>
            value.Length <= MaxUsernameLength ? value : value.Substring(0, MaxUsernameLength);
    }
}
