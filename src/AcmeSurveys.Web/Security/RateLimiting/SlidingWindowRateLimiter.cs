using System;
using System.Runtime.Caching;

namespace AcmeSurveys.Web.Security.RateLimiting
{
    // Sliding window counter: estimado = anterior × (fracción visible de la ventana anterior) + actual.
    // Estado en memoria por instancia; con varios servidores se movería a Redis o al gateway.
    public sealed class SlidingWindowRateLimiter : IDisposable
    {
        private readonly MemoryCache _counters = new MemoryCache("AcmeSurveys.RateLimiting");

        public RateLimitDecision TryAcquire(string key, int permitLimit, TimeSpan window, DateTime nowUtc)
        {
            var candidate = new WindowCounter();
            var counter = (WindowCounter)_counters.AddOrGetExisting(key, candidate,
                new CacheItemPolicy { SlidingExpiration = window + window }) ?? candidate;

            lock (counter)
            {
                var windowTicks = window.Ticks;
                var currentWindowStart = nowUtc.Ticks - nowUtc.Ticks % windowTicks;

                if (counter.WindowStartTicks != currentWindowStart)
                {
                    var isAdjacent = currentWindowStart - counter.WindowStartTicks == windowTicks;
                    counter.PreviousCount = isAdjacent ? counter.CurrentCount : 0;
                    counter.CurrentCount = 0;
                    counter.WindowStartTicks = currentWindowStart;
                }

                var elapsed = (double)(nowUtc.Ticks - currentWindowStart) / windowTicks;
                var estimated = counter.PreviousCount * (1 - elapsed) + counter.CurrentCount;

                if (estimated + 1 > permitLimit)
                {
                    var retryAfter = TimeSpan.FromTicks(currentWindowStart + windowTicks - nowUtc.Ticks);
                    return new RateLimitDecision(false, retryAfter < TimeSpan.FromSeconds(1) ? TimeSpan.FromSeconds(1) : retryAfter);
                }

                counter.CurrentCount++;
                return new RateLimitDecision(true, TimeSpan.Zero);
            }
        }

        public void Dispose() => _counters.Dispose();

        private sealed class WindowCounter
        {
            public long WindowStartTicks;
            public int CurrentCount;
            public int PreviousCount;
        }
    }
}
