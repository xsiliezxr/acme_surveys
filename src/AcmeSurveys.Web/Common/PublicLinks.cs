using System;

namespace AcmeSurveys.Web.Common
{
    public static class PublicLinks
    {
        public const string RoutePrefix = "s";

        public static string ForToken(Uri requestUri, Guid publicToken) =>
            requestUri.GetLeftPart(UriPartial.Authority) + "/" + RoutePrefix + "/" + publicToken.ToString("D");
    }
}
