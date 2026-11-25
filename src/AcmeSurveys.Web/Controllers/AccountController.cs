using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using AcmeSurveys.Application.Abstractions;
using AcmeSurveys.Application.Auth;
using AcmeSurveys.Web.Models;
using AcmeSurveys.Web.Security;
using Microsoft.Owin.Security;

namespace AcmeSurveys.Web.Controllers
{
    [RoutePrefix("account")]
    public sealed class AccountController : AppControllerBase
    {
        private readonly AuthenticationService _authentication;
        private readonly IClock _clock;

        public AccountController(AuthenticationService authentication, IClock clock)
        {
            _authentication = authentication;
            _clock = clock;
        }

        private IAuthenticationManager Authentication => HttpContext.GetOwinContext().Authentication;

        [AllowAnonymous]
        [HttpGet, Route("login")]
        public ActionResult Login(string returnUrl)
        {
            if (User.Identity.IsAuthenticated) return RedirectToLocal(returnUrl);
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [AllowAnonymous]
        [HttpPost, Route("login")]
        public async Task<ActionResult> Login(LoginViewModel model)
        {
            var result = await _authentication.AuthenticateAsync(model.Username, model.Password, ClientIp);
            if (result.IsFailure)
            {
                ModelState.AddModelError(string.Empty, result.Errors[0].Message);
                model.Password = null;
                return View(model);
            }

            Authentication.SignOut(AppAuthentication.CookieScheme);
            Authentication.SignIn(
                new AuthenticationProperties { IsPersistent = false, AllowRefresh = true },
                AppAuthentication.CreateIdentity(result.Value, _clock.UtcNow));

            return RedirectToLocal(model.ReturnUrl);
        }

        [HttpPost, Route("logout")]
        public ActionResult Logout()
        {
            Authentication.SignOut(AppAuthentication.CookieScheme);
            return RedirectToAction("Login");
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction("Index", "Surveys");
        }
    }
}
