using System;
using System.Threading.Tasks;
using AcmeSurveys.Application.Abstractions;
using AcmeSurveys.Application.Auth;
using AcmeSurveys.Domain.Common;
using AcmeSurveys.Domain.Users;
using AcmeSurveys.Infrastructure.Security;
using AcmeSurveys.UnitTests.TestDoubles;
using Xunit;

namespace AcmeSurveys.UnitTests.Application
{
    public class AuthenticationServiceTests
    {
        private const string Password = "Correcta#2026";
        private const int MaxFailedAttempts = 3;
        private readonly FakeClock _clock = new FakeClock(new DateTime(2026, 11, 24, 12, 0, 0, DateTimeKind.Utc));
        private readonly InMemoryUserRepository _users = new InMemoryUserRepository();
        private readonly NullAuditLog _audit = new NullAuditLog();
        private readonly AuthenticationService _service;

        public AuthenticationServiceTests()
        {
            var hasher = new Pbkdf2PasswordHasher(Pbkdf2PasswordHasher.MinimumIterations);
            _users.Add(new User(1, Guid.NewGuid(), "admin", hasher.Hash(Password), 0, null));
            _service = new AuthenticationService(_users, hasher, _clock, _audit,
                new LockoutPolicy(MaxFailedAttempts, TimeSpan.FromMinutes(15)));
        }

        private async Task ExhaustAttemptsAsync()
        {
            for (var i = 0; i < MaxFailedAttempts; i++)
            {
                await _service.AuthenticateAsync("admin", "incorrecta", "ip");
            }
        }

        [Fact]
        public async Task AuthenticateAsync_UsernameWithDifferentCaseAndSpaces_Succeeds()
        {
            var result = await _service.AuthenticateAsync(" ADMIN ", Password, "127.0.0.1");

            Assert.True(result.IsSuccess);
            Assert.Equal(1, result.Value.UserId);
        }

        [Fact]
        public async Task AuthenticateAsync_UnknownUserOrWrongPassword_ReturnsSameGenericError()
        {
            var unknown = await _service.AuthenticateAsync("nadie", Password, "ip");
            var wrong = await _service.AuthenticateAsync("admin", "incorrecta", "ip");

            Assert.Equal(ErrorCodes.InvalidCredentials, unknown.Errors[0].Code);
            Assert.Equal(unknown.Errors[0].Message, wrong.Errors[0].Message);
        }

        [Fact]
        public async Task AuthenticateAsync_MaxFailedAttemptsReached_LocksAccountEvenWithCorrectPassword()
        {
            await ExhaustAttemptsAsync();

            var result = await _service.AuthenticateAsync("admin", Password, "ip");

            Assert.True(result.IsFailure);
            Assert.Contains(SecurityEvent.AccountLockedOut, _audit.Events);
        }

        [Fact]
        public async Task AuthenticateAsync_LockoutPeriodElapsed_Succeeds()
        {
            await ExhaustAttemptsAsync();
            _clock.UtcNow = _clock.UtcNow.AddMinutes(16);

            var result = await _service.AuthenticateAsync("admin", Password, "ip");

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task AuthenticateAsync_OversizedPassword_Fails()
        {
            var result = await _service.AuthenticateAsync("admin", new string('a', 10000), "ip");

            Assert.True(result.IsFailure);
        }
    }
}
