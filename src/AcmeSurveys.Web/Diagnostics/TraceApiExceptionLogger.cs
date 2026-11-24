using System.Web.Http.ExceptionHandling;

namespace AcmeSurveys.Web.Diagnostics
{
    public sealed class TraceApiExceptionLogger : ExceptionLogger
    {
        public override void Log(ExceptionLoggerContext context)
        {
            ApplicationErrorLog.Write(context.Exception, "API " + context.Request.Method + " " + context.Request.RequestUri.AbsolutePath);
        }
    }
}
