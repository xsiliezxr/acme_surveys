using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using AcmeSurveys.Application.Auth;
using AcmeSurveys.Application.Results;
using AcmeSurveys.Application.Surveys;
using AcmeSurveys.Domain.Common;
using AcmeSurveys.Domain.FieldValues;
using AcmeSurveys.Domain.Surveys;
using AcmeSurveys.Web.Api.Models;
using AcmeSurveys.Web.Common;
using Swashbuckle.Swagger.Annotations;

namespace AcmeSurveys.Web.Api.Controllers
{
    /// <summary>
    /// Gestión de las encuestas del usuario autenticado y lectura de sus resultados. Usa los mismos casos de uso
    /// que la UI: misma verificación de dueño, mismas reglas y mismo control de concurrencia.
    /// </summary>
    [RoutePrefix("api/v1/surveys")]
    public sealed class SurveysApiController : ApiControllerBase
    {
        private readonly SurveyManagementService _surveys;
        private readonly SurveyResultsService _results;

        public SurveysApiController(AuthenticationService authentication, SurveyManagementService surveys, SurveyResultsService results)
            : base(authentication)
        {
            _surveys = surveys;
            _results = results;
        }

        /// <summary>Lista las encuestas activas del usuario autenticado, de la más reciente a la más antigua.</summary>
        [HttpGet, Route("")]
        [ResponseType(typeof(List<SurveySummaryResponse>))]
        public async Task<IHttpActionResult> List()
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            var surveys = await _surveys.ListAsync(user.UserId);
            return Ok(surveys.Select(s => new SurveySummaryResponse
            {
                Id = s.ExternalId,
                Name = s.Name,
                Description = s.Description,
                FieldCount = s.FieldCount,
                ResponseCount = s.ResponseCount,
                PublicUrl = PublicLinks.ForToken(Request.RequestUri, s.PublicToken),
                CreatedAtUtc = s.CreatedAtUtc,
                UpdatedAtUtc = s.UpdatedAtUtc
            }).ToList());
        }

        /// <summary>Devuelve la definición de una encuesta con sus campos en orden.</summary>
        /// <param name="id">Identificador público (UUID) de la encuesta.</param>
        [HttpGet, Route("{id:guid}")]
        [ResponseType(typeof(SurveyDetailResponse))]
        [SwaggerResponse(HttpStatusCode.NotFound, "La encuesta no existe o es de otro usuario (no se distingue).", typeof(ApiError))]
        public async Task<IHttpActionResult> Get(Guid id)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            var result = await _surveys.GetAsync(user.UserId, id);
            if (result.IsFailure) return FromFailure(result);
            return Ok(ToDetail(result.Value));
        }

        /// <summary>Crea una encuesta.</summary>
        /// <remarks>Ejemplo: <c>{ "name": "Clima laboral", "fields": [ { "name": "edad", "title": "Edad", "type": "number", "isRequired": true } ] }</c></remarks>
        [HttpPost, Route("")]
        [SwaggerResponseRemoveDefaults]
        [SwaggerResponse(HttpStatusCode.Created, "Encuesta creada; la cabecera Location apunta a ella.", typeof(SurveyDetailResponse))]
        [SwaggerResponse(HttpStatusCode.BadRequest, "Datos inválidos; el detalle por dato llega en errors.", typeof(ApiError))]
        public async Task<IHttpActionResult> Create([FromBody] SaveSurveyRequest request)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            var command = ToCommand(request, null);
            if (command.IsFailure) return FromFailure(command);

            var created = await _surveys.CreateAsync(user.UserId, command.Value);
            if (created.IsFailure) return FromFailure(created);

            var survey = await _surveys.GetAsync(user.UserId, created.Value);
            if (survey.IsFailure) return FromFailure(survey);

            var location = new Uri(Request.RequestUri.GetLeftPart(UriPartial.Path).TrimEnd('/') + "/" + created.Value);
            return Created(location, ToDetail(survey.Value));
        }

        /// <summary>Reemplaza el nombre, la descripción y los campos de una encuesta.</summary>
        /// <remarks>
        /// Requiere el <c>rowVersion</c> del último GET. Si otra sesión guardó antes responde 409 y hay que volver
        /// a leer la encuesta. Con respuestas registradas no se puede cambiar el tipo ni eliminar campos.
        /// </remarks>
        /// <param name="id">Identificador público (UUID) de la encuesta.</param>
        /// <param name="request">Definición completa: los campos que no se envían se eliminan.</param>
        [HttpPut, Route("{id:guid}")]
        [ResponseType(typeof(SurveyDetailResponse))]
        [SwaggerResponse(HttpStatusCode.BadRequest, "Datos inválidos o falta rowVersion.", typeof(ApiError))]
        [SwaggerResponse(HttpStatusCode.NotFound, "La encuesta no existe o es de otro usuario (no se distingue).", typeof(ApiError))]
        [SwaggerResponse(HttpStatusCode.Conflict, "La encuesta cambió desde el último GET.", typeof(ApiError))]
        public async Task<IHttpActionResult> Update(Guid id, [FromBody] SaveSurveyRequest request)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            var current = await _surveys.GetAsync(user.UserId, id);
            if (current.IsFailure) return FromFailure(current);

            var command = ToCommand(request, current.Value);
            if (command.IsFailure) return FromFailure(command);
            if (command.Value.RowVersion == null)
                return FromFailure(Result.Failure(Error.Validation("RowVersion", "Envíe el rowVersion obtenido en el último GET.")));

            var updated = await _surveys.UpdateAsync(user.UserId, id, command.Value);
            if (updated.IsFailure) return FromFailure(updated);

            var survey = await _surveys.GetAsync(user.UserId, id);
            if (survey.IsFailure) return FromFailure(survey);
            return Ok(ToDetail(survey.Value));
        }

        /// <summary>Elimina una encuesta (borrado lógico: deja de listarse y su link público deja de funcionar).</summary>
        /// <param name="id">Identificador público (UUID) de la encuesta.</param>
        [HttpDelete, Route("{id:guid}")]
        [SwaggerResponseRemoveDefaults]
        [SwaggerResponse(HttpStatusCode.NoContent, "Encuesta eliminada.")]
        [SwaggerResponse(HttpStatusCode.NotFound, "La encuesta no existe o es de otro usuario (no se distingue).", typeof(ApiError))]
        public async Task<IHttpActionResult> Delete(Guid id)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            var result = await _surveys.DeleteAsync(user.UserId, id);
            if (result.IsFailure) return FromFailure(result);
            return StatusCode(HttpStatusCode.NoContent);
        }

        /// <summary>Genera un link público nuevo; el anterior deja de funcionar.</summary>
        /// <param name="id">Identificador público (UUID) de la encuesta.</param>
        [HttpPost, Route("{id:guid}/public-link")]
        [ResponseType(typeof(PublicLinkResponse))]
        [SwaggerResponse(HttpStatusCode.NotFound, "La encuesta no existe o es de otro usuario (no se distingue).", typeof(ApiError))]
        public async Task<IHttpActionResult> RotatePublicLink(Guid id)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            var rotated = await _surveys.RotatePublicLinkAsync(user.UserId, id);
            if (rotated.IsFailure) return FromFailure(rotated);

            var survey = await _surveys.GetAsync(user.UserId, id);
            if (survey.IsFailure) return FromFailure(survey);
            return Ok(new PublicLinkResponse { PublicUrl = PublicLinks.ForToken(Request.RequestUri, survey.Value.PublicToken) });
        }

        /// <summary>Devuelve una página de respuestas descifradas, de la más reciente a la más antigua.</summary>
        /// <remarks>Solo se descifran los valores de la página pedida. Los valores llegan tipados según el campo.</remarks>
        /// <param name="id">Identificador público (UUID) de la encuesta.</param>
        /// <param name="page">Número de página, desde 1 (un valor menor se toma como 1).</param>
        /// <param name="pageSize">Respuestas por página, de 1 a 100 (por defecto 25; un valor mayor se limita a 100).</param>
        [HttpGet, Route("{id:guid}/results")]
        [ResponseType(typeof(SurveyResultsResponse))]
        [SwaggerResponse(HttpStatusCode.NotFound, "La encuesta no existe o es de otro usuario (no se distingue).", typeof(ApiError))]
        public async Task<IHttpActionResult> Results(Guid id, int page = 1, int pageSize = SurveyResultsService.DefaultPageSize)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            var result = await _results.GetPageAsync(user.UserId, id, page, pageSize);
            if (result.IsFailure) return FromFailure(result);

            var data = result.Value;
            return Ok(new SurveyResultsResponse
            {
                SurveyId = data.ExternalId,
                Name = data.Name,
                Page = data.Page,
                PageSize = data.PageSize,
                TotalCount = data.TotalCount,
                TotalPages = data.TotalPages,
                Fields = data.Columns.Select(c => new ResultColumnResponse
                {
                    Name = c.Name,
                    Title = c.Title,
                    Type = FieldTypes.ToApi(c.Type)
                }).ToList(),
                Items = data.Rows.Select(row => new ResultItemResponse
                {
                    SubmittedAtUtc = row.SubmittedAtUtc,
                    HasIntegrityError = row.HasIntegrityError,
                    Values = ToJsonValues(data.Columns, row.Values)
                }).ToList()
            });
        }

        private SurveyDetailResponse ToDetail(SurveyDetailsDto survey) => new SurveyDetailResponse
        {
            Id = survey.ExternalId,
            Name = survey.Name,
            Description = survey.Description,
            PublicUrl = PublicLinks.ForToken(Request.RequestUri, survey.PublicToken),
            HasResponses = survey.HasResponses,
            RowVersion = survey.RowVersion == null ? null : Convert.ToBase64String(survey.RowVersion),
            Fields = survey.Fields.Select(f => new SurveyFieldResponse
            {
                Name = f.Name,
                Title = f.Title,
                Type = FieldTypes.ToApi(f.Type),
                IsRequired = f.IsRequired
            }).ToList()
        };

        /// <summary>
        /// Al editar, el campo existente se reconoce por su nombre: el id interno nunca sale de la API.
        /// Un tipo desconocido se pasa como valor no definido para que el dominio lo reporte junto con el resto.
        /// </summary>
        private static Result<SaveSurveyCommand> ToCommand(SaveSurveyRequest request, SurveyDetailsDto current)
        {
            if (request == null)
                return Result.Failure<SaveSurveyCommand>(Error.Validation(string.Empty, "El cuerpo de la petición es obligatorio."));

            var definitions = (request.Fields ?? new List<SurveyFieldRequest>()).Select(field =>
            {
                if (field == null) return null;

                FieldType type;
                if (!FieldTypes.TryParse(field.Type, out type)) type = default(FieldType);

                var existingId = current?.Fields
                    .FirstOrDefault(f => string.Equals(f.Name, field.Name?.Trim(), StringComparison.OrdinalIgnoreCase))?.Id;
                return new FieldDefinition(existingId, field.Name, field.Title, type, field.IsRequired);
            }).ToList();

            return Result.Success(new SaveSurveyCommand(request.Name, request.Description, definitions, DecodeRowVersion(request.RowVersion)));
        }

        private static byte[] DecodeRowVersion(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            try
            {
                return Convert.FromBase64String(value);
            }
            catch (FormatException)
            {
                return null;
            }
        }

        private static IDictionary<string, object> ToJsonValues(IReadOnlyList<ResultColumnDto> columns, object[] values)
        {
            var map = new Dictionary<string, object>(columns.Count, StringComparer.Ordinal);
            for (var i = 0; i < columns.Count; i++)
            {
                var value = values[i];

                map[columns[i].Name] = value is DateTime date
                    ? date.ToString(DateFieldHandler.CanonicalFormat, CultureInfo.InvariantCulture)
                    : value;
            }
            return map;
        }
    }
}
