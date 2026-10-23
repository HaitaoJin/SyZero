using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using SyZero.Runtime.Session;

namespace SyZero.Feign
{
    public class AuthenticationFeignHandler : DelegatingHandler
    {
        private readonly ISySession _session;

        public AuthenticationFeignHandler(ISySession session, HttpMessageHandler? innerHandler = null) : base(innerHandler ?? new HttpClientHandler())
        {
            _session = session;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Headers.Authorization == null)
            {
                if (!string.IsNullOrWhiteSpace(_session.Token))
                {
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _session.Token);
                }
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
