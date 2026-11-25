using System;
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
        protected IHttpActionResult FromFailure(Result result)
        {
            if (result.HasError(ErrorCodes.NotFound))
                return Problem(HttpStatusCode.NotFound, ErrorCodes.NotFound, result.Errors[0].Message);
            if (result.HasError(ErrorCodes.Conflict))
                return Problem(HttpStatusCode.Conflict, ErrorCodes.Conflict, result.Errors[0].Message);
            return Problem(HttpStatusCode.BadRequest, ErrorCodes.Validation, result.Errors[0].Message);
        }
    }
}
