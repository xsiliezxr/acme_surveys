using System;

namespace AcmeSurveys.Domain.Users
{
    public sealed class LockoutPolicy
    {
        public LockoutPolicy(int maxFailedAttempts, TimeSpan lockoutDuration)
        {
            if (maxFailedAttempts <= 0) throw new ArgumentOutOfRangeException(nameof(maxFailedAttempts));
            if (lockoutDuration <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(lockoutDuration));
            MaxFailedAttempts = maxFailedAttempts;
            LockoutDuration = lockoutDuration;
        }

        public int MaxFailedAttempts { get; }
        public TimeSpan LockoutDuration { get; }

        public static LockoutPolicy Default => new LockoutPolicy(5, TimeSpan.FromMinutes(15));
    }
}
