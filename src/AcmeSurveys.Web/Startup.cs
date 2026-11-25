using System;
using System.Globalization;
using System.Threading.Tasks;
using AcmeSurveys.Web;
using AcmeSurveys.Web.Security;
using AcmeSurveys.Web.Security.RateLimiting;
using Microsoft.Owin;
using Microsoft.Owin.Extensions;
using Microsoft.Owin.Host.SystemWeb;
using Microsoft.Owin.Security.Cookies;
using Owin;

[assembly: OwinStartup(typeof(Startup))]

namespace AcmeSurveys.Web
{
    public sealed class Startup
    {
        private static readonly PathString ApiPath = new PathString("/api");

        public void Configuration(IAppBuilder app)
        {
            var settings = AppSettings.Current;

            // Orden: rate limit (antes de gastar CPU/BD) -> HSTS -> autenticación por cookie.
            app.Use<RateLimitMiddleware>(RateLimitPolicy.Defaults, new SlidingWindowRateLimiter());
            app.Use<HstsMiddleware>();

            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = AppAuthentication.CookieScheme,
                CookieName = AppAuthentication.CookieName,
                LoginPath = new PathString("/account/login"),
                CookieHttpOnly = true,
                CookieSecure = CookieSecureOption.Always,
                CookieSameSite = SameSiteMode.Lax,
                CookiePath = "/",
                ExpireTimeSpan = settings.SessionIdleTimeout,
                SlidingExpiration = true,

                // Evita que System.Web sobrescriba la cookie de Katana en la misma respuesta.
                CookieManager = new SystemWebChunkingCookieManager(),
                Provider = new CookieAuthenticationProvider
                {
                    OnValidateIdentity = context => EnforceAbsoluteTimeout(context, settings.SessionAbsoluteTimeout),
                    OnApplyRedirect = context =>
                    {
                        if (!context.Request.Path.StartsWithSegments(ApiPath))
                            context.Response.Redirect(context.RedirectUri);
                    }
                }
            });

            app.UseStageMarker(PipelineStage.Authenticate);
        }

        private static Task EnforceAbsoluteTimeout(CookieValidateIdentityContext context, TimeSpan absoluteTimeout)
        {
            var claim = context.Identity?.FindFirst(AppClaimTypes.AuthenticatedAt);
            DateTime authenticatedAt;

            var isValid = claim != null &&
                          DateTime.TryParse(claim.Value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out authenticatedAt) &&
                          DateTime.UtcNow - authenticatedAt.ToUniversalTime() <= absoluteTimeout;

            if (!isValid)
            {
                context.RejectIdentity();
                context.OwinContext.Authentication.SignOut(AppAuthentication.CookieScheme);
            }
            return Task.FromResult(0);
        }
    }
}
