using System;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Http;
using AcmeSurveys.Application.Abstractions.Security;
using AcmeSurveys.Application.Auth;
using AcmeSurveys.Domain.Common;
using AcmeSurveys.Web.Api.Models;

namespace AcmeSurveys.Web.Api.Controllers
{
    /// <summary>Comportamiento común de los controllers de la API (evita repetirlo en cada uno).</summary>
    public abstract class ApiControllerBase : ApiController
    {
        private readonly AuthenticationService _authentication;

        protected ApiControllerBase(AuthenticationService authentication)
        {
            _authentication = authentication;
        }

        /// <summary>
        /// Usuario dueño del JWT, verificado contra la BD (que exista y no esté bloqueado).
        /// El JWT solo trae el UUID externo; el id interno se resuelve aquí, en el servidor.
        /// </summary>
        protected async Task<AuthenticatedUser> GetCurrentUserAsync()
        {
            var subject = (User as ClaimsPrincipal)?.FindFirst(AccessTokenClaims.Subject)?.Value;
            Guid externalId;
            return Guid.TryParse(subject, out externalId)
                ? await _authentication.FindActiveUserAsync(externalId)
                : null;
        }

        protected IHttpActionResult Problem(HttpStatusCode status, string error, string message) =>
            Content(status, new ApiError(error, message));

        /// <summary>Traduce un Result fallido a la respuesta HTTP adecuada.</summary>
        /// <param name="result">Resultado fallido del caso de uso.</param>
        /// <param name="targetMap">Convierte el target del dominio en la ruta del dato en el JSON.</param>
        protected IHttpActionResult FromFailure(Result result, Func<string, string> targetMap = null)
        {
            if (result.HasError(ErrorCodes.NotFound))
                return Problem(HttpStatusCode.NotFound, ErrorCodes.NotFound, result.Errors[0].Message);
            if (result.HasError(ErrorCodes.Conflict))
                return Problem(HttpStatusCode.Conflict, ErrorCodes.Conflict, result.Errors[0].Message);

            var map = targetMap ?? ToCamelCasePath;
            var details = result.Errors.Select(e => new ApiFieldError(map(e.Target ?? string.Empty), e.Message)).ToList();
            return Content(HttpStatusCode.BadRequest, new ApiError(ErrorCodes.Validation, "Los datos enviados no son válidos.", details));
        }

        /// <summary>"Fields[0].Name" del dominio pasa a "fields[0].name", igual que el JSON en camelCase.</summary>
        private static string ToCamelCasePath(string target)
        {
            if (string.IsNullOrEmpty(target)) return string.Empty;
            return string.Join(".", target.Split('.').Select(s => s.Length == 0 ? s : char.ToLowerInvariant(s[0]) + s.Substring(1)));
        }
    }
}
