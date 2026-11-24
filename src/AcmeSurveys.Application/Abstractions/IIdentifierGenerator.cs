using System;

namespace AcmeSurveys.Application.Abstractions
{
    public interface IIdentifierGenerator
    {
        Guid NewRandomUuid();
    }
}
