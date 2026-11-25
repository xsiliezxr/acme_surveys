using System;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace AcmeSurveys.Web.Security
{
    public sealed class HttpsRedirectionMiddleware : OwinMiddleware
    {
        private static readonly PathString ApiPath = new PathString("/api");
        private readonly int _httpsPort;

        public HttpsRedirectionMiddleware(OwinMiddleware next, int httpsPort) : base(next)
        {
            _httpsPort = httpsPort;
        }

        public override Task Invoke(IOwinContext context)
        {
            // La API no se redirige: responde 403 para no reenviar credenciales que ya viajaron en claro.
            if (context.Request.IsSecure || context.Request.Path.StartsWithSegments(ApiPath))
                return Next.Invoke(context);

            var target = new UriBuilder(context.Request.Uri) { Scheme = Uri.UriSchemeHttps, Port = _httpsPort };
            var isSafeMethod = context.Request.Method == "GET" || context.Request.Method == "HEAD";
            context.Response.StatusCode = isSafeMethod ? 301 : 308;
            context.Response.Headers["Location"] = target.Uri.AbsoluteUri;
            return Task.FromResult(0);
        }
    }
}
