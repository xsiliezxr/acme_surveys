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
using AcmeSurveys.Domain.FieldValues;
using AcmeSurveys.Domain.Surveys;
using AcmeSurveys.Web.Api.Models;
using AcmeSurveys.Web.Common;
using Swashbuckle.Swagger.Annotations;

namespace AcmeSurveys.Web.Api.Controllers
{
    /// <summary>
    /// API de solo lectura para integraciones de reporting (BI, ETL). Reutiliza EXACTAMENTE los
    /// mismos casos de uso que la UI: misma verificación de dueño, mismo descifrado y paginación.
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

            var survey = result.Value;
            return Ok(new SurveyDetailResponse
            {
                Id = survey.ExternalId,
                Name = survey.Name,
                Description = survey.Description,
                PublicUrl = PublicLinks.ForToken(Request.RequestUri, survey.PublicToken),
                HasResponses = survey.HasResponses,
                Fields = survey.Fields.Select(f => new SurveyFieldResponse
                {
                    Name = f.Name,
                    Title = f.Title,
                    Type = ToApiType(f.Type),
                    IsRequired = f.IsRequired
                }).ToList()
            });
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
                    Type = ToApiType(c.Type)
                }).ToList(),
                Items = data.Rows.Select(row => new ResultItemResponse
                {
                    SubmittedAtUtc = row.SubmittedAtUtc,
                    HasIntegrityError = row.HasIntegrityError,
                    Values = ToJsonValues(data.Columns, row.Values)
                }).ToList()
            });
        }

        /// <summary>Contrato público estable: "text" | "number" | "date" (independiente del nombre del enum en C#).</summary>
        private static string ToApiType(FieldType type) => type.ToString().ToLowerInvariant();

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
