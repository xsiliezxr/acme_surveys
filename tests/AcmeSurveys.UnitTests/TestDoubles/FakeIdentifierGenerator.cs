using System;
using AcmeSurveys.Application.Abstractions;

namespace AcmeSurveys.UnitTests.TestDoubles
{
    internal sealed class FakeIdentifierGenerator : IIdentifierGenerator
    {
        public Guid NewRandomUuid() => Guid.NewGuid();
    }
}
