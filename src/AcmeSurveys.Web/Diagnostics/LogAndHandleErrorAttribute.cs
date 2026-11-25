using System;
using System.Web.Mvc;

namespace AcmeSurveys.Web.Diagnostics
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class LogAndHandleErrorAttribute : HandleErrorAttribute
    {
        public override void OnException(ExceptionContext filterContext)
        {
            if (!filterContext.ExceptionHandled)
            {
                var request = filterContext.HttpContext.Request;
                var correlationId = ApplicationErrorLog.Write(filterContext.Exception, request.HttpMethod + " " + request.Path);
                filterContext.Controller.ViewData["CorrelationId"] = correlationId;
            }
            base.OnException(filterContext);
        }
    }
}
