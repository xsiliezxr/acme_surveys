using System.Collections.Generic;
using Newtonsoft.Json;

namespace AcmeSurveys.Web.Api.Models
{
    /// <summary>Cuerpo de todas las respuestas de error de negocio de la API.</summary>
    public sealed class ApiError
    {
        public ApiError(string error, string message, IReadOnlyList<ApiFieldError> errors = null)
        {
            Error = error;
            Message = message;
            Errors = errors != null && errors.Count > 0 ? errors : null;
        }

        /// <summary>Código estable para programas: validation, not_found, conflict, invalid_credentials.</summary>
        public string Error { get; }

        /// <summary>Mensaje legible para personas (puede cambiar; no lo interprete un programa).</summary>
        public string Message { get; }

        /// <summary>Detalle por dato cuando <c>error</c> es <c>validation</c>.</summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public IReadOnlyList<ApiFieldError> Errors { get; }
    }

    /// <summary>Error de validación de un dato concreto.</summary>
    public sealed class ApiFieldError
    {
        public ApiFieldError(string target, string message)
        {
            Target = target;
            Message = message;
        }

        /// <summary>Ruta del dato con error, por ejemplo <c>name</c> o <c>fields[0].type</c>; vacío si es general.</summary>
        public string Target { get; }

        public string Message { get; }
    }
}
