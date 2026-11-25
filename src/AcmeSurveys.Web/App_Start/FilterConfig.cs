using System.Web.Mvc;
using AcmeSurveys.Application.Abstractions;
using AcmeSurveys.Web.Diagnostics;
using AcmeSurveys.Web.Security;

namespace AcmeSurveys.Web
{
    public static class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters, ISecurityAuditLog auditLog)
        {
            filters.Add(new LogAndHandleErrorAttribute());
            filters.Add(new RequireHttpsAttribute());
            filters.Add(new AuthorizeAttribute());
            filters.Add(new ValidateAntiForgeryTokenOnPostAttribute(auditLog));
            filters.Add(new NoStoreCacheAttribute());
        }
    }
}
