using System;
using System.Web;
using System.Web.Mvc;

namespace AcmeSurveys.Web.Security
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class NoStoreCacheAttribute : ActionFilterAttribute
    {
        public override void OnResultExecuting(ResultExecutingContext filterContext)
        {
            var cache = filterContext.HttpContext.Response.Cache;
            cache.SetCacheability(HttpCacheability.NoCache);
            cache.SetNoStore();
            cache.SetExpires(DateTime.UtcNow.AddYears(-1));
            base.OnResultExecuting(filterContext);
        }
    }
}
