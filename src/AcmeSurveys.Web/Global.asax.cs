using System;
using System.Diagnostics;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Helpers;
using System.Web.Hosting;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using AcmeSurveys.Application.Abstractions;
using AcmeSurveys.Application.Abstractions.Security;
using AcmeSurveys.Infrastructure.Logging;
using AcmeSurveys.Web.Diagnostics;
using Autofac;

namespace AcmeSurveys.Web
{
    public class MvcApplication : HttpApplication
    {
        private static int _urlsAnnounced;

        protected void Application_Start()
        {
            ConfigureLogging();

            var settings = AppSettings.Current;
            var container = DependencyConfig.Build(settings);

            GlobalConfiguration.Configure(config =>
            {
                WebApiConfig.Register(config, container.Resolve<IAccessTokenService>());
                if (settings.SwaggerEnabled) SwaggerConfig.Register(config);
            });
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters, container.Resolve<ISecurityAuditLog>());
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            ViewEngines.Engines.Clear();
            ViewEngines.Engines.Add(new RazorViewEngine());

            MvcHandler.DisableMvcResponseHeader = true;

            AntiForgeryConfig.RequireSsl = true;
            AntiForgeryConfig.CookieName = "__Host-AcmeXsrf";
            AntiForgeryConfig.UniqueClaimTypeIdentifier = ClaimTypes.NameIdentifier;
            AntiForgeryConfig.SuppressXFrameOptionsHeader = true;
        }

        protected void Application_BeginRequest()
        {
            if (Interlocked.Exchange(ref _urlsAnnounced, 1) == 0)
                AnnounceUrls(Request.Url.GetLeftPart(UriPartial.Authority), AppSettings.Current.SwaggerEnabled);
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            var exception = Server.GetLastError();
            if (exception != null && !(exception is HttpException httpException && httpException.GetHttpCode() == 404))
            {
                ApplicationErrorLog.Write(exception, Request.HttpMethod + " " + Request.Path);
            }
        }

        private static void AnnounceUrls(string baseUrl, bool swaggerEnabled)
        {
            var message = new StringBuilder()
                .AppendLine("Acme Surveys escuchando en " + baseUrl + "/")
                .AppendLine("  API REST:   " + baseUrl + "/api/v1");
            if (swaggerEnabled)
            {
                message.AppendLine("  Swagger UI: " + baseUrl + "/" + SwaggerConfig.UiPath)
                       .AppendLine("  OpenAPI:    " + baseUrl + "/" + SwaggerConfig.DocumentPath);
            }

            Console.Write(message.ToString());
            Trace.Write(message.ToString());
        }

        private static void ConfigureLogging()
        {
            var directory = HostingEnvironment.MapPath("~/App_Data/logs");
            Directory.CreateDirectory(directory);

            TraceSecurityAuditLog.AddListener(new TextWriterTraceListener(Path.Combine(directory, "security.log")));
            ApplicationErrorLog.AddListener(new TextWriterTraceListener(Path.Combine(directory, "errors.log")));
        }
    }
}
