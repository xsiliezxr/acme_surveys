using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Description;
using Swashbuckle.Swagger;

namespace AcmeSurveys.Web.Api.Documentation
{
    public sealed class SecurityRequirementsOperationFilter : IOperationFilter
    {
        public const string SchemeName = "Bearer";

        public void Apply(Operation operation, SchemaRegistry schemaRegistry, ApiDescription apiDescription)
        {
            if (operation.responses == null)
                operation.responses = new Dictionary<string, Response>();

            AddResponse(operation, "429", "Demasiadas peticiones desde esta IP (rate limit). Reintentar tras la cabecera Retry-After.");

            if (IsAnonymous(apiDescription))
                return;

            operation.security = new List<IDictionary<string, IEnumerable<string>>>
            {
                new Dictionary<string, IEnumerable<string>> { [SchemeName] = new string[0] }
            };
            AddResponse(operation, "401", "Falta el token, es inválido o expiró (cabecera WWW-Authenticate: Bearer).");
        }

        private static bool IsAnonymous(ApiDescription apiDescription) =>
            apiDescription.ActionDescriptor.GetCustomAttributes<AllowAnonymousAttribute>().Any() ||
            apiDescription.ActionDescriptor.ControllerDescriptor.GetCustomAttributes<AllowAnonymousAttribute>().Any();

        private static void AddResponse(Operation operation, string statusCode, string description)
        {
            if (!operation.responses.ContainsKey(statusCode))
                operation.responses[statusCode] = new Response { description = description };
        }
    }
}
