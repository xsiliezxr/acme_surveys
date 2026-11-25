namespace AcmeSurveys.Web.Api.Models
{
    /// <summary>Cuerpo de todas las respuestas de error de negocio de la API.</summary>
    public sealed class ApiError
    {
        public ApiError(string error, string message)
        {
            Error = error;
            Message = message;
        }

        /// <summary>Código estable para programas: validation, not_found, conflict, invalid_credentials.</summary>
        public string Error { get; }

        /// <summary>Mensaje legible para personas (puede cambiar; no lo interprete un programa).</summary>
        public string Message { get; }
    }
}
