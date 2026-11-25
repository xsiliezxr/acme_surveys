using System.ComponentModel.DataAnnotations;

namespace AcmeSurveys.Web.Api.Models
{
    /// <summary>Credenciales para obtener un JWT.</summary>
    public sealed class TokenRequest
    {
        /// <summary>Nombre de usuario.</summary>
        [Required, StringLength(50)]
        public string Username { get; set; }

        /// <summary>Contraseña. Solo viaja por HTTPS y nunca se registra en bitácoras.</summary>
        [Required, StringLength(128)]
        public string Password { get; set; }
    }
}
