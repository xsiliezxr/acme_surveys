using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace AcmeSurveys.Web.Security.RateLimiting
{
    public sealed class RateLimitMiddleware : OwinMiddleware
    {
        private readonly IReadOnlyList<RateLimitPolicy> _policies;
        private readonly SlidingWindowRateLimiter _limiter;

        public RateLimitMiddleware(OwinMiddleware next, IReadOnlyList<RateLimitPolicy> policies, SlidingWindowRateLimiter limiter)
            : base(next)
        {
            _policies = policies;
            _limiter = limiter;
        }

        public override Task Invoke(IOwinContext context)
        {
            var clientIp = context.Request.RemoteIpAddress ?? "unknown";
            var now = DateTime.UtcNow;

            foreach (var policy in _policies)
            {
                if (!policy.AppliesTo(context.Request)) continue;

                var decision = _limiter.TryAcquire(policy.Name + "|" + clientIp, policy.PermitLimit, policy.Window, now);
                if (!decision.IsAllowed) return RejectAsync(context, decision.RetryAfter);
            }

            return Next.Invoke(context);
        }

        private static Task RejectAsync(IOwinContext context, TimeSpan retryAfter)
        {
            var seconds = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
            var response = context.Response;
            response.StatusCode = 429;
            response.ReasonPhrase = "Too Many Requests";
            response.Headers["Retry-After"] = seconds;
            response.Headers["Cache-Control"] = "no-store";

            if (context.Request.Path.StartsWithSegments(new PathString("/api")))
            {
                response.ContentType = "application/json; charset=utf-8";
                return response.WriteAsync("{\"error\":\"rate_limited\",\"retryAfterSeconds\":" + seconds + "}");
            }

            response.ContentType = "text/html; charset=utf-8";
            return response.WriteAsync(
                "<!DOCTYPE html><html lang=\"es\"><head><meta charset=\"utf-8\"><title>Demasiadas solicitudes</title></head>" +
                "<body><h1>Demasiadas solicitudes</h1><p>Espere " + seconds + " segundos e intente de nuevo.</p></body></html>");
        }
    }
}
