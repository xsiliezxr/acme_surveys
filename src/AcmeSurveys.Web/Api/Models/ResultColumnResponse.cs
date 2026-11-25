namespace AcmeSurveys.Web.Api.Models
{
    /// <summary>Columna de resultados: solo lo necesario para interpretar los valores (no incluye reglas de captura).</summary>
    public sealed class ResultColumnResponse
    {
        public string Name { get; set; }

        public string Title { get; set; }

        /// <summary>Tipo de dato: text, number o date.</summary>
        public string Type { get; set; }
    }
}
