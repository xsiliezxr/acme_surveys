using System.Web.Mvc;
using System.Web.Routing;

namespace AcmeSurveys.Web
{
    public static class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.LowercaseUrls = true;
            routes.AppendTrailingSlash = false;

            routes.MapMvcAttributeRoutes();

            routes.MapRoute(
                name: "NotFound",
                url: "{*url}",
                defaults: new { controller = "Errors", action = "NotFound" });
        }
    }
}
