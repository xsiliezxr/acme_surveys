namespace AcmeSurveys.Web.Api.Models
{
    /// <summary>Campo de una encuesta al crear o editar.</summary>
    public sealed class SurveyFieldRequest
    {
        /// <summary>
        /// Nombre técnico, único en la encuesta. Al editar identifica al campo existente: cambiarlo equivale a
        /// eliminar el campo y crear otro.
        /// </summary>
        public string Name { get; set; }

        /// <summary>Texto que ve el encuestado.</summary>
        public string Title { get; set; }

        /// <summary>Tipo de dato: text, number o date.</summary>
        public string Type { get; set; }

        public bool IsRequired { get; set; }
    }
}
