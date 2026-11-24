using System;

namespace AcmeSurveys.Domain.Users
{
    public sealed class User
    {
        public User(int id, Guid externalId, string username, PasswordHash password,
            int failedLoginCount, DateTime? lockoutEndUtc)
        {
            Id = id;
            ExternalId = externalId;
            Username = username;
            Password = password ?? throw new ArgumentNullException(nameof(password));
            FailedLoginCount = failedLoginCount;
            LockoutEndUtc = lockoutEndUtc;
        }

        public int Id { get; }

        public Guid ExternalId { get; }

        public string Username { get; }

        public PasswordHash Password { get; }

        public int FailedLoginCount { get; }

        public DateTime? LockoutEndUtc { get; }

        public bool IsLockedOut(DateTime nowUtc) => LockoutEndUtc.HasValue && LockoutEndUtc.Value > nowUtc;
    }
}
