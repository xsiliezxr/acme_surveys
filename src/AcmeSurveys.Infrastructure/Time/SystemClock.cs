using System;
using AcmeSurveys.Application.Abstractions;

namespace AcmeSurveys.Infrastructure.Time
{
    public sealed class SystemClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
