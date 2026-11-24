using System;
using System.Globalization;
using System.Security.Claims;
using AcmeSurveys.Application.Auth;

namespace AcmeSurveys.Web.Security
{
    public static class AppAuthentication
    {
        public const string CookieScheme = "AcmeCookie";

        public const string CookieName = "__Host-AcmeAuth";

        public static ClaimsIdentity CreateIdentity(AuthenticatedUser user, DateTime nowUtc)
        {
            var identity = new ClaimsIdentity(CookieScheme, ClaimTypes.Name, ClaimTypes.Role);
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.ExternalId.ToString()));
            identity.AddClaim(new Claim(ClaimTypes.Name, user.Username));
            identity.AddClaim(new Claim(AppClaimTypes.UserId, user.UserId.ToString(CultureInfo.InvariantCulture)));
            identity.AddClaim(new Claim(AppClaimTypes.AuthenticatedAt, nowUtc.ToString("o", CultureInfo.InvariantCulture)));
            return identity;
        }
    }
}
