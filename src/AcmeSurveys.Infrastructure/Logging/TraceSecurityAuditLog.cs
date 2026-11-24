using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using AcmeSurveys.Application.Abstractions;

namespace AcmeSurveys.Infrastructure.Logging
{
    public sealed class TraceSecurityAuditLog : ISecurityAuditLog
    {
        public const string SourceName = "AcmeSurveys.Security";
        private const int MaxFieldLength = 200;

        private static readonly TraceSource Source = new TraceSource(SourceName, SourceLevels.Information);

        private readonly IClock _clock;

        public TraceSecurityAuditLog(IClock clock)
        {
            _clock = clock;
        }

        public static void AddListener(TraceListener listener) => Source.Listeners.Add(listener);

        public void Write(SecurityEvent securityEvent, string subject, string detail = null)
        {
            var level = IsWarning(securityEvent) ? TraceEventType.Warning : TraceEventType.Information;
            var message = string.Format(CultureInfo.InvariantCulture,
                "{0:o} event={1} subject={2} detail={3}",
                _clock.UtcNow, securityEvent, Sanitize(subject), Sanitize(detail));

            Source.TraceEvent(level, (int)securityEvent, message);
            Source.Flush();
        }

        private static bool IsWarning(SecurityEvent e) =>
            e == SecurityEvent.LoginFailed || e == SecurityEvent.LoginRejectedLockedOut ||
            e == SecurityEvent.AccountLockedOut || e == SecurityEvent.AccessDenied ||
            e == SecurityEvent.CiphertextIntegrityFailure;

        private static string Sanitize(string value)
        {
            if (string.IsNullOrEmpty(value)) return "-";

            var builder = new StringBuilder(Math.Min(value.Length, MaxFieldLength));
            foreach (var c in value)
            {
                if (builder.Length == MaxFieldLength) break;
                builder.Append(char.IsControl(c) ? '_' : c);
            }
            return builder.ToString();
        }
    }
}
