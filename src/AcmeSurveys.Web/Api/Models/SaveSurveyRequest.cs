using System.Collections.Generic;

namespace AcmeSurveys.Web.Api.Models
{
    /// <summary>Datos para crear o editar una encuesta.</summary>
    public sealed class SaveSurveyRequest
    {
        /// <summary>Nombre visible (máximo 150 caracteres).</summary>
        public string Name { get; set; }

        /// <summary>Descripción opcional (máximo 1000 caracteres).</summary>
        public string Description { get; set; }

        /// <summary>Campos en el orden en que se muestran (de 1 a 50).</summary>
        public List<SurveyFieldRequest> Fields { get; set; }

        /// <summary>
        /// Solo al editar: el <c>rowVersion</c> recibido en el GET. Si otra sesión guardó antes, la API responde 409.
        /// </summary>
        public string RowVersion { get; set; }
    }
}
