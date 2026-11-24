using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Filters;
using AcmeSurveys.Application.Abstractions.Security;

namespace AcmeSurveys.Web.Api.Filters
{
    public sealed class JwtAuthenticationFilter : IAuthenticationFilter
    {
        private const string Scheme = "Bearer";
        private readonly IAccessTokenService _tokens;

        public JwtAuthenticationFilter(IAccessTokenService tokens)
        {
            _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        }

        public bool AllowMultiple => false;

        public Task AuthenticateAsync(HttpAuthenticationContext context, CancellationToken cancellationToken)
        {
            context.Principal = new ClaimsPrincipal(new ClaimsIdentity());

            var header = context.Request.Headers.Authorization;
            if (header == null || !string.Equals(header.Scheme, Scheme, StringComparison.OrdinalIgnoreCase))
                return Task.FromResult(0);

            var principal = _tokens.Validate(header.Parameter);
            if (principal == null)
            {
                context.ErrorResult = new BearerChallengeResult(context.Request, "invalid_token");
                return Task.FromResult(0);
            }

            context.Principal = principal;
            return Task.FromResult(0);
        }

        public Task ChallengeAsync(HttpAuthenticationChallengeContext context, CancellationToken cancellationToken)
        {
            context.Result = new AddChallengeOnUnauthorizedResult(context.Result);
            return Task.FromResult(0);
        }

        private sealed class BearerChallengeResult : IHttpActionResult
        {
            private readonly HttpRequestMessage _request;
            private readonly string _error;

            public BearerChallengeResult(HttpRequestMessage request, string error)
            {
                _request = request;
                _error = error;
            }

            public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
            {
                var response = _request.CreateResponse(HttpStatusCode.Unauthorized, new { error = _error });
                response.Headers.WwwAuthenticate.Add(new AuthenticationHeaderValue(Scheme, "error=\"" + _error + "\""));
                return Task.FromResult(response);
            }
        }

        private sealed class AddChallengeOnUnauthorizedResult : IHttpActionResult
        {
            private readonly IHttpActionResult _inner;

            public AddChallengeOnUnauthorizedResult(IHttpActionResult inner)
            {
                _inner = inner;
            }

            public async Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
            {
                var response = await _inner.ExecuteAsync(cancellationToken).ConfigureAwait(false);
                if (response.StatusCode == HttpStatusCode.Unauthorized && response.Headers.WwwAuthenticate.Count == 0)
                    response.Headers.WwwAuthenticate.Add(new AuthenticationHeaderValue(Scheme));
                return response;
            }
        }
    }
}
