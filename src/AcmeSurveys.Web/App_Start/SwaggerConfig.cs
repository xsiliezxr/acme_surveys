using System;
using System.IO;
using System.Web.Hosting;
using System.Web.Http;
using AcmeSurveys.Web.Api.Documentation;
using Swashbuckle.Application;

namespace AcmeSurveys.Web
{
    public static class SwaggerConfig
    {
        public const string UiPath = "swagger";
        public const string DocumentPath = "swagger/docs/v1";
        private const string ApiControllerSuffix = "Api";

        public static void Register(HttpConfiguration config)
        {
            config.EnableSwagger("swagger/docs/{apiVersion}", c =>
            {
                c.SingleApiVersion("v1", "Acme Surveys API")
                    .Description("API REST de solo lectura para integraciones (BI, reportes). " +
                                 "1) Obtenga un token con POST /api/v1/auth/token. " +
                                 "2) Pulse Authorize y escriba: Bearer {token}.");

                c.Schemes(new[] { "https" });

                var xmlComments = HostingEnvironment.MapPath("~/bin/AcmeSurveys.Web.xml");
                if (xmlComments != null && File.Exists(xmlComments))
                    c.IncludeXmlComments(xmlComments);

                c.GroupActionsBy(api => TrimSuffix(api.ActionDescriptor.ControllerDescriptor.ControllerName, ApiControllerSuffix));

                c.ApiKey(SecurityRequirementsOperationFilter.SchemeName)
                    .Name("Authorization")
                    .In("header")
                    .Description("JWT de POST /api/v1/auth/token. Escriba: Bearer {token}");
                c.OperationFilter<SecurityRequirementsOperationFilter>();
            });
        }

        private static string TrimSuffix(string value, string suffix) =>
            value.EndsWith(suffix, StringComparison.Ordinal) ? value.Substring(0, value.Length - suffix.Length) : value;
    }
}
