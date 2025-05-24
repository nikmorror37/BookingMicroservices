using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace BookingService.API.Infrastructure.Handlers
{
    public class BearerTokenHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _accessor;

        public BearerTokenHandler(IHttpContextAccessor accessor)
        {
            _accessor = accessor;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var bearer = _accessor.HttpContext?
                .Request.Headers["Authorization"]
                .ToString();

            if (!string.IsNullOrEmpty(bearer))
            {
                // bearer is like "Bearer {token}"
                if (AuthenticationHeaderValue.TryParse(bearer, out var header))
                    request.Headers.Authorization = header;
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
} 