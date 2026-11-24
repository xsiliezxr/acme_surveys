using System;
using System.Globalization;
using System.Security.Claims;
using System.Security.Principal;

namespace AcmeSurveys.Web.Security
{
    public static class PrincipalExtensions
    {
        public static int GetUserId(this IPrincipal principal)
        {
            var claim = (principal as ClaimsPrincipal)?.FindFirst(AppClaimTypes.UserId);
            int id;
            if (claim == null || !int.TryParse(claim.Value, NumberStyles.None, CultureInfo.InvariantCulture, out id))
                throw new InvalidOperationException("The current principal has no user id claim.");
            return id;
        }
    }
}
