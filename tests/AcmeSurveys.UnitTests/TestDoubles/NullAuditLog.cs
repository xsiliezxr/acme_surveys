using System.Collections.Generic;
using AcmeSurveys.Application.Abstractions;

namespace AcmeSurveys.UnitTests.TestDoubles
{
    internal sealed class NullAuditLog : ISecurityAuditLog
    {
        public List<SecurityEvent> Events { get; } = new List<SecurityEvent>();

        public void Write(SecurityEvent securityEvent, string subject, string detail = null) => Events.Add(securityEvent);
    }
}
