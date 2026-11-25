using System;
using System.Collections.Generic;

namespace AcmeSurveys.Web.Api.Models
{
    /// <summary>Página de respuestas descifradas de una encuesta.</summary>
    public sealed class SurveyResultsResponse
    {
        /// <summary>Identificador público de la encuesta.</summary>
        public Guid SurveyId { get; set; }

        public string Name { get; set; }

        public int Page { get; set; }

        public int PageSize { get; set; }

        /// <summary>Total de respuestas (todas las páginas).</summary>
        public long TotalCount { get; set; }

        public int TotalPages { get; set; }

        /// <summary>Columnas: una por campo, en orden.</summary>
        public IReadOnlyList<ResultColumnResponse> Fields { get; set; }

        /// <summary>Respuestas de la página, de la más reciente a la más antigua.</summary>
        public IReadOnlyList<ResultItemResponse> Items { get; set; }
    }
}
