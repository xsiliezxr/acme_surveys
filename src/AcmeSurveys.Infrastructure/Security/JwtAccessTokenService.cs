using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AcmeSurveys.Application.Abstractions;
using AcmeSurveys.Application.Abstractions.Security;
using AcmeSurveys.Application.Auth;
using Microsoft.IdentityModel.Tokens;

namespace AcmeSurveys.Infrastructure.Security
{
    public sealed class JwtAccessTokenService : IAccessTokenService
    {
        public const string SubjectClaim = AccessTokenClaims.Subject;
        public const string NameClaim = AccessTokenClaims.Name;
        private const int MaxTokenLength = 4096;

        private readonly JwtOptions _options;
        private readonly IClock _clock;
        private readonly SymmetricSecurityKey _key;
        private readonly TokenValidationParameters _validation;

        public JwtAccessTokenService(JwtOptions options, IClock clock)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _key = new SymmetricSecurityKey(options.SigningKey);

            _validation = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = options.Issuer,
                ValidateAudience = true,
                ValidAudience = options.Audience,
                ValidateLifetime = true,
                RequireExpirationTime = true,
                RequireSignedTokens = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _key,
                ClockSkew = TimeSpan.FromSeconds(30),
                NameClaimType = NameClaim
            };
        }

        public AccessToken Issue(AuthenticatedUser user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            var now = _clock.UtcNow;
            var expires = now.Add(_options.Lifetime);

            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = _options.Issuer,
                Audience = _options.Audience,
                IssuedAt = now,
                NotBefore = now,
                Expires = expires,
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(SubjectClaim, user.ExternalId.ToString()),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
                    new Claim(NameClaim, user.Username)
                }),
                SigningCredentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256)
            };

            var handler = CreateHandler();
            return new AccessToken(handler.WriteToken(handler.CreateToken(descriptor)), expires);
        }

        public ClaimsPrincipal Validate(string token)
        {
            if (string.IsNullOrWhiteSpace(token) || token.Length > MaxTokenLength) return null;

            try
            {
                SecurityToken validated;
                var principal = CreateHandler().ValidateToken(token, _validation, out validated);

                var jwt = validated as JwtSecurityToken;
                // Algoritmo fijado a HS256: bloquea "alg: none" y la confusión de algoritmos.
                if (jwt == null || !string.Equals(jwt.Header.Alg, SecurityAlgorithms.HmacSha256, StringComparison.Ordinal))
                    return null;

                return principal;
            }
            catch (SecurityTokenException)
            {
                return null;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        private static JwtSecurityTokenHandler CreateHandler()
        {
            var handler = new JwtSecurityTokenHandler();

            handler.InboundClaimTypeMap.Clear();
            handler.OutboundClaimTypeMap.Clear();
            return handler;
        }
    }
}
