using System.Threading.Tasks;
using Microsoft.Owin;

namespace AcmeSurveys.Web.Security
{
    public sealed class HstsMiddleware : OwinMiddleware
    {
        private const string HeaderValue = "max-age=31536000; includeSubDomains";

        public HstsMiddleware(OwinMiddleware next) : base(next) { }

        public override Task Invoke(IOwinContext context)
        {
            if (context.Request.IsSecure && !IsLoopback(context.Request.Uri.Host))
            {
                context.Response.Headers["Strict-Transport-Security"] = HeaderValue;
            }
            return Next.Invoke(context);
        }

        private static bool IsLoopback(string host) =>
            host == "localhost" || host == "127.0.0.1" || host == "[::1]";
    }
}
