using System.Collections.Generic;

namespace AcmeSurveys.Web.Api.Models
{
    /// <summary>Respuesta de un encuestado.</summary>
    public sealed class SubmitResponseRequest
    {
        /// <summary>
        /// Valor por nombre de campo. Número con punto decimal (<c>"3.5"</c>) y fecha en formato <c>yyyy-MM-dd</c>.
        /// Los campos opcionales se pueden omitir.
        /// </summary>
        public Dictionary<string, string> Values { get; set; }
    }
}
