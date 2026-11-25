namespace AcmeSurveys.Web.Api.Models
{
    /// <summary>Link público vigente de una encuesta.</summary>
    public sealed class PublicLinkResponse
    {
        /// <summary>URL para llenar la encuesta sin iniciar sesión. El link anterior deja de funcionar.</summary>
        public string PublicUrl { get; set; }
    }
}
