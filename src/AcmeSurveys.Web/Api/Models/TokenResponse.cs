using System;

namespace AcmeSurveys.Web.Api.Models
{
    /// <summary>Token de acceso emitido tras un login correcto.</summary>
    public sealed class TokenResponse
    {
        /// <summary>JWT firmado con HS256. Enviarlo como <c>Authorization: Bearer {token}</c>.</summary>
        public string AccessToken { get; set; }

        /// <summary>Siempre <c>Bearer</c>.</summary>
        public string TokenType { get; set; }

        /// <summary>Segundos de vida del token.</summary>
        public int ExpiresIn { get; set; }

        /// <summary>Momento de expiración en UTC.</summary>
        public DateTime ExpiresAtUtc { get; set; }
    }
}
