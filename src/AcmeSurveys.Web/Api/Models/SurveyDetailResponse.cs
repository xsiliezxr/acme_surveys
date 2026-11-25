using System;
using System.Collections.Generic;

namespace AcmeSurveys.Web.Api.Models
{
    /// <summary>Definición completa de una encuesta.</summary>
    public sealed class SurveyDetailResponse
    {
        /// <summary>Identificador público (UUID v4).</summary>
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        /// <summary>Link público para llenar la encuesta sin iniciar sesión.</summary>
        public string PublicUrl { get; set; }

        /// <summary>Si ya tiene respuestas, no se pueden cambiar tipos ni eliminar campos.</summary>
        public bool HasResponses { get; set; }

        /// <summary>Campos en el orden en que se muestran.</summary>
        public IReadOnlyList<SurveyFieldResponse> Fields { get; set; }
    }
}
