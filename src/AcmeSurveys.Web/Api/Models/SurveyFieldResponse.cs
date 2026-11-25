namespace AcmeSurveys.Web.Api.Models
{
    /// <summary>Campo dinámico de una encuesta.</summary>
    public sealed class SurveyFieldResponse
    {
        /// <summary>Nombre técnico, único dentro de la encuesta.</summary>
        public string Name { get; set; }

        /// <summary>Texto que ve el encuestado.</summary>
        public string Title { get; set; }

        /// <summary>Tipo de dato: text, number o date.</summary>
        public string Type { get; set; }

        public bool IsRequired { get; set; }
    }
}
