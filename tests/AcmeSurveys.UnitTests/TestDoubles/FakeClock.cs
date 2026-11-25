using System;
using AcmeSurveys.Application.Abstractions;

namespace AcmeSurveys.UnitTests.TestDoubles
{
    internal sealed class FakeClock : IClock
    {
        public FakeClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; set; }
    }
}
