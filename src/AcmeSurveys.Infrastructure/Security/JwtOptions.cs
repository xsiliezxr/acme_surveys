using System;

namespace AcmeSurveys.Infrastructure.Security
{
    public sealed class JwtOptions
    {
        public const int MinimumKeySize = 32;

        public JwtOptions(string issuer, string audience, byte[] signingKey, TimeSpan lifetime)
        {
            if (string.IsNullOrWhiteSpace(issuer)) throw new ArgumentException("Issuer is required.", nameof(issuer));
            if (string.IsNullOrWhiteSpace(audience)) throw new ArgumentException("Audience is required.", nameof(audience));
            if (signingKey == null || signingKey.Length < MinimumKeySize)
                throw new ArgumentException("The JWT signing key must be at least 256 bits.", nameof(signingKey));
            if (lifetime <= TimeSpan.Zero || lifetime > TimeSpan.FromHours(1))
                throw new ArgumentOutOfRangeException(nameof(lifetime), "Access tokens must be short-lived (max 1 hour).");

            Issuer = issuer;
            Audience = audience;
            SigningKey = signingKey;
            Lifetime = lifetime;
        }

        public string Issuer { get; }
        public string Audience { get; }
        public byte[] SigningKey { get; }
        public TimeSpan Lifetime { get; }
    }
}
