using System;
using Microsoft.Owin;

namespace AcmeSurveys.Web.Security.RateLimiting
{
    public sealed class RateLimitPolicy
    {
        public RateLimitPolicy(string name, string method, string pathPrefix, int permitLimit, TimeSpan window)
        {
            Name = name;
            Method = method;
            PathPrefix = string.IsNullOrEmpty(pathPrefix) ? PathString.Empty : new PathString(pathPrefix);
            PermitLimit = permitLimit;
            Window = window;
        }

        public string Name { get; }

        public string Method { get; }

        public PathString PathPrefix { get; }
        public int PermitLimit { get; }
        public TimeSpan Window { get; }

        public bool AppliesTo(IOwinRequest request) =>
            (Method == null || string.Equals(request.Method, Method, StringComparison.OrdinalIgnoreCase)) &&
            (!PathPrefix.HasValue || request.Path.StartsWithSegments(PathPrefix));

        public static RateLimitPolicy[] Defaults => new[]
        {
            new RateLimitPolicy("login",         "POST", "/account/login",      5,   TimeSpan.FromMinutes(1)),
            new RateLimitPolicy("api-token",     "POST", "/api/v1/auth/token",  5,   TimeSpan.FromMinutes(1)),
            new RateLimitPolicy("public-submit", "POST", "/s",                  10,  TimeSpan.FromMinutes(1)),
            new RateLimitPolicy("api",           null,   "/api",                60,  TimeSpan.FromMinutes(1)),
            new RateLimitPolicy("global",        null,   null,                  300, TimeSpan.FromMinutes(1))
        };
    }
}
