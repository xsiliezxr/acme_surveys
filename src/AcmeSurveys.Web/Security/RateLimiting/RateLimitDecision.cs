using System;

namespace AcmeSurveys.Web.Security.RateLimiting
{
    public struct RateLimitDecision
    {
        public RateLimitDecision(bool isAllowed, TimeSpan retryAfter)
        {
            IsAllowed = isAllowed;
            RetryAfter = retryAfter;
        }

        public bool IsAllowed { get; }
        public TimeSpan RetryAfter { get; }
    }
}
