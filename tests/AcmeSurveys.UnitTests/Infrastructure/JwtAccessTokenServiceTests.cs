using System;
using System.Linq;
using AcmeSurveys.Application.Abstractions.Security;
using AcmeSurveys.Application.Auth;
using AcmeSurveys.Infrastructure.Security;
using AcmeSurveys.UnitTests.TestDoubles;
using Xunit;

namespace AcmeSurveys.UnitTests.Infrastructure
{
    public class JwtAccessTokenServiceTests
    {
        private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(15);
        private readonly FakeClock _clock = new FakeClock(DateTime.UtcNow);
        private readonly AuthenticatedUser _user = new AuthenticatedUser(1, Guid.NewGuid(), "admin");
        private readonly JwtAccessTokenService _service;

        public JwtAccessTokenServiceTests()
        {
            _service = CreateService(keyByte: 7);
        }

        private JwtAccessTokenService CreateService(byte keyByte) =>
            new JwtAccessTokenService(
                new JwtOptions("acme", "acme-api", Enumerable.Repeat(keyByte, 32).ToArray(), Lifetime), _clock);

        [Fact]
        public void Validate_IssuedToken_ReturnsPrincipalWithExternalIdAsSubject()
        {
            var token = _service.Issue(_user).Token;

            var principal = _service.Validate(token);

            Assert.NotNull(principal);
            Assert.Equal(_user.ExternalId.ToString(), principal.FindFirst(AccessTokenClaims.Subject).Value);
        }

        [Fact]
        public void Validate_TamperedSignature_ReturnsNull()
        {
            var token = _service.Issue(_user).Token;

            var tampered = token.Substring(0, token.Length - 2) + "xx";

            Assert.Null(_service.Validate(tampered));
        }

        [Fact]
        public void Validate_TokenSignedWithAnotherKey_ReturnsNull()
        {
            var token = _service.Issue(_user).Token;

            var principal = CreateService(keyByte: 8).Validate(token);

            Assert.Null(principal);
        }

        [Fact]
        public void Validate_ExpiredToken_ReturnsNull()
        {
            var now = _clock.UtcNow;
            _clock.UtcNow = now.AddHours(-2);
            var token = _service.Issue(_user).Token;
            _clock.UtcNow = now;

            var principal = _service.Validate(token);

            Assert.Null(principal);
        }
    }
}
