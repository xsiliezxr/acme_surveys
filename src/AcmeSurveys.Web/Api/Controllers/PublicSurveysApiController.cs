using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using AcmeSurveys.Application.Auth;
using AcmeSurveys.Application.Responses;
using AcmeSurveys.Web.Api.Models;
using Swashbuckle.Swagger.Annotations;

namespace AcmeSurveys.Web.Api.Controllers
{
    /// <summary>Llenado anónimo de encuestas mediante su link público (mismo caso de uso que el formulario web).</summary>
    [AllowAnonymous]
    [RoutePrefix("api/v1/public/surveys")]
    public sealed class PublicSurveysApiController : ApiControllerBase
    {
        private readonly PublicSurveyService _publicSurveys;

        public PublicSurveysApiController(AuthenticationService authentication, PublicSurveyService publicSurveys)
            : base(authentication)
        {
            _publicSurveys = publicSurveys;
        }

        /// <summary>Devuelve el formulario de una encuesta a partir del token de su link público.</summary>
        /// <param name="token">UUID final del link público (<c>/s/{token}</c>).</param>
        [HttpGet, Route("{token:guid}")]
        [ResponseType(typeof(PublicSurveyResponse))]
        [SwaggerResponse(HttpStatusCode.NotFound, "El link no existe, se regeneró o la encuesta se eliminó.", typeof(ApiError))]
        public async Task<IHttpActionResult> Get(Guid token)
        {
            var result = await _publicSurveys.GetFormAsync(token);
            if (result.IsFailure) return FromFailure(result);

            var form = result.Value;
            return Ok(new PublicSurveyResponse
            {
                Name = form.Name,
                Description = form.Description,
                Fields = form.Fields.Select(f => new SurveyFieldResponse
                {
                    Name = f.Name,
                    Title = f.Title,
                    Type = FieldTypes.ToApi(f.Type),
                    IsRequired = f.IsRequired
                }).ToList()
            });
        }

        /// <summary>Registra una respuesta. Los valores se validan y se guardan cifrados.</summary>
        /// <remarks>
        /// Ejemplo: <c>{ "values": { "edad": "34", "ingreso": "2024-03-01" } }</c>. Limitado a 10 envíos por minuto por IP.
        /// </remarks>
        /// <param name="token">UUID final del link público (<c>/s/{token}</c>).</param>
        /// <param name="request">Valores por nombre de campo.</param>
        [HttpPost, Route("{token:guid}/responses")]
        [SwaggerResponseRemoveDefaults]
        [SwaggerResponse(HttpStatusCode.Created, "Respuesta registrada.")]
        [SwaggerResponse(HttpStatusCode.BadRequest, "Valores inválidos; el detalle llega en errors (target values.{campo}).", typeof(ApiError))]
        [SwaggerResponse(HttpStatusCode.NotFound, "El link no existe, se regeneró o la encuesta se eliminó.", typeof(ApiError))]
        [SwaggerResponse(HttpStatusCode.Conflict, "La encuesta cambió mientras se llenaba.", typeof(ApiError))]
        public async Task<IHttpActionResult> Submit(Guid token, [FromBody] SubmitResponseRequest request)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (request?.Values != null)
            {
                foreach (var pair in request.Values)
                    values[pair.Key] = pair.Value;
            }

            var result = await _publicSurveys.SubmitAsync(token, values);
            if (result.IsFailure)
                return FromFailure(result, target => string.IsNullOrEmpty(target) ? string.Empty : "values." + target);
            return StatusCode(HttpStatusCode.Created);
        }
    }
}
