using System.Security.Claims;
using AcmeSurveys.Application.Auth;

namespace AcmeSurveys.Application.Abstractions.Security
{
    public interface IAccessTokenService
    {
        AccessToken Issue(AuthenticatedUser user);

        ClaimsPrincipal Validate(string token);
    }
}
