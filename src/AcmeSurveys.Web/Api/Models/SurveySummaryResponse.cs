using System;

namespace AcmeSurveys.Web.Api.Models
{
    /// <summary>Encuesta en el listado (sin campos ni respuestas).</summary>
    public sealed class SurveySummaryResponse
    {
        /// <summary>Identificador público (UUID v4). Nunca se expone el id interno.</summary>
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public int FieldCount { get; set; }

        public long ResponseCount { get; set; }

        /// <summary>Link público para llenar la encuesta sin iniciar sesión.</summary>
        public string PublicUrl { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime UpdatedAtUtc { get; set; }
    }
}
