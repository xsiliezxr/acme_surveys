using System.Collections.Generic;

namespace AcmeSurveys.Web.Api.Models
{
    /// <summary>Formulario público de una encuesta (sin datos del dueño ni respuestas).</summary>
    public sealed class PublicSurveyResponse
    {
        public string Name { get; set; }

        public string Description { get; set; }

        /// <summary>Campos en el orden en que se muestran.</summary>
        public IReadOnlyList<SurveyFieldResponse> Fields { get; set; }
    }
}
