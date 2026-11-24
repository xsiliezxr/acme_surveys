using System;

namespace AcmeSurveys.Application.Abstractions.Security
{
    public sealed class AccessToken
    {
        public AccessToken(string token, DateTime expiresAtUtc)
        {
            Token = token;
            ExpiresAtUtc = expiresAtUtc;
        }

        public string Token { get; }
        public DateTime ExpiresAtUtc { get; }
    }
}
