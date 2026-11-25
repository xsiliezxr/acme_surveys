using System;
using System.Diagnostics;
using System.Globalization;

namespace AcmeSurveys.Web.Diagnostics
{
    public static class ApplicationErrorLog
    {
        private static readonly TraceSource Source = new TraceSource("AcmeSurveys.Errors", SourceLevels.Error);

        public static void AddListener(TraceListener listener) => Source.Listeners.Add(listener);

        public static string Write(Exception exception, string context)
        {
            var correlationId = Guid.NewGuid().ToString("N").Substring(0, 12);
            Source.TraceEvent(TraceEventType.Error, 0, string.Format(CultureInfo.InvariantCulture,
                "{0:o} id={1} context={2}{3}{4}", DateTime.UtcNow, correlationId, context, Environment.NewLine, exception));
            Source.Flush();
            return correlationId;
        }
    }
}
