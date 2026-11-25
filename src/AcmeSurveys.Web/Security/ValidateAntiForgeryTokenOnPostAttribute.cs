using System;
using System.Web.Helpers;
using System.Web.Mvc;
using AcmeSurveys.Application.Abstractions;

namespace AcmeSurveys.Web.Security
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class ValidateAntiForgeryTokenOnPostAttribute : FilterAttribute, IAuthorizationFilter
    {
        private readonly ISecurityAuditLog _auditLog;

        public ValidateAntiForgeryTokenOnPostAttribute(ISecurityAuditLog auditLog)
        {
            _auditLog = auditLog ?? throw new ArgumentNullException(nameof(auditLog));
        }

        public void OnAuthorization(AuthorizationContext filterContext)
        {
            var httpContext = filterContext.HttpContext;
            if (!string.Equals(httpContext.Request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            try
            {
                AntiForgery.Validate();
            }
            catch (HttpAntiForgeryException)
            {
                _auditLog.Write(SecurityEvent.AntiForgeryValidationFailed, httpContext.Request.UserHostAddress, httpContext.Request.Path);

                httpContext.Response.TrySkipIisCustomErrors = true;
                filterContext.Result = new ViewResult { ViewName = "BadRequest" };
                httpContext.Response.StatusCode = 400;
            }
        }
    }
}
