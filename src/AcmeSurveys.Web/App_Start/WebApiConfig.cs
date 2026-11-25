using System.Web.Http;
using System.Web.Http.ExceptionHandling;
using AcmeSurveys.Application.Abstractions.Security;
using AcmeSurveys.Web.Api.Filters;
using AcmeSurveys.Web.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace AcmeSurveys.Web
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config, IAccessTokenService tokens)
        {
            config.MapHttpAttributeRoutes();

            config.Formatters.Remove(config.Formatters.XmlFormatter);
            var json = config.Formatters.JsonFormatter.SerializerSettings;
            json.ContractResolver = new CamelCasePropertyNamesContractResolver();
            json.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
            json.NullValueHandling = NullValueHandling.Include;
            json.MaxDepth = 32;

            config.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Never;
            config.Services.Add(typeof(IExceptionLogger), new TraceApiExceptionLogger());

            config.Filters.Add(new RequireHttpsApiAttribute());
            config.Filters.Add(new JwtAuthenticationFilter(tokens));
            config.Filters.Add(new AuthorizeAttribute());
        }
    }
}
