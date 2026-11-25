using System;

namespace AcmeSurveys.Application.Auth
{
    public sealed class AuthenticatedUser
    {
        public AuthenticatedUser(int userId, Guid externalId, string username)
        {
            UserId = userId;
            ExternalId = externalId;
            Username = username;
        }

        public int UserId { get; }
        public Guid ExternalId { get; }
        public string Username { get; }
    }
}
