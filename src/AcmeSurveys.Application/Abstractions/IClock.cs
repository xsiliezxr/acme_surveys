using System;

namespace AcmeSurveys.Application.Abstractions
{
    public interface IClock
    {
        DateTime UtcNow { get; }
    }
}
