using System;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Description;
using AcmeSurveys.Application.Abstractions.Security;
using AcmeSurveys.Application.Auth;
using AcmeSurveys.Domain.Common;
using AcmeSurveys.Web.Api.Models;
using Swashbuckle.Swagger.Annotations;

namespace AcmeSurveys.Web.Api.Controllers
{
    /// <summary>Emisión de tokens de acceso para clientes de la API.</summary>
    [RoutePrefix("api/v1/auth")]
    public sealed class AuthApiController : ApiControllerBase
    {
        private readonly AuthenticationService _authentication;
        private readonly IAccessTokenService _tokens;

        public AuthApiController(AuthenticationService authentication, IAccessTokenService tokens) : base(authentication)
        {
            _authentication = authentication;
            _tokens = tokens;
        }

        /// <summary>Obtiene un JWT de acceso (válido 15 minutos).</summary>
        /// <remarks>
        /// Limitado a 5 intentos por minuto por IP. Usuario inexistente, contraseña incorrecta y cuenta
        /// bloqueada devuelven el mismo 401, a propósito (no revela qué usuarios existen).
        /// </remarks>
        [AllowAnonymous]
        [HttpPost, Route("token")]
        [ResponseType(typeof(TokenResponse))]
        [SwaggerResponse(HttpStatusCode.BadRequest, "Faltan usuario o contraseña.", typeof(ApiError))]
        [SwaggerResponse(HttpStatusCode.Unauthorized, "Credenciales inválidas o cuenta bloqueada (mismo mensaje).", typeof(ApiError))]
        public async Task<IHttpActionResult> Token([FromBody] TokenRequest request)
        {
            if (request == null || !ModelState.IsValid)
                return Problem(HttpStatusCode.BadRequest, ErrorCodes.Validation, "Usuario y contraseña son obligatorios.");

            var clientIp = HttpContext.Current?.Request.UserHostAddress;
            var result = await _authentication.AuthenticateAsync(request.Username, request.Password, clientIp);
            if (result.IsFailure)
                return Problem(HttpStatusCode.Unauthorized, ErrorCodes.InvalidCredentials, AuthenticationService.GenericFailureMessage);

            var token = _tokens.Issue(result.Value);
            return Ok(new TokenResponse
            {
                AccessToken = token.Token,
                TokenType = "Bearer",
                ExpiresIn = (int)Math.Max(0, (token.ExpiresAtUtc - DateTime.UtcNow).TotalSeconds),
                ExpiresAtUtc = token.ExpiresAtUtc
            });
        }
    }
}
