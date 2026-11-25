namespace AcmeSurveys.Application.Abstractions
{
    public interface ISecurityAuditLog
    {
        void Write(SecurityEvent securityEvent, string subject, string detail = null);
    }
}
