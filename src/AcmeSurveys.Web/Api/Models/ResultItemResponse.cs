using System;
using System.Collections.Generic;

namespace AcmeSurveys.Web.Api.Models
{
    /// <summary>Una respuesta enviada por un encuestado.</summary>
    public sealed class ResultItemResponse
    {
        public DateTime SubmittedAtUtc { get; set; }

        /// <summary>Valores por nombre de campo: texto (string), número (number), fecha ("yyyy-MM-dd") o null si no se respondió.</summary>
        public IDictionary<string, object> Values { get; set; }

        /// <summary>true si algún valor no pasó la verificación HMAC (fue alterado en la BD); ese valor llega como null.</summary>
        public bool HasIntegrityError { get; set; }
    }
}
