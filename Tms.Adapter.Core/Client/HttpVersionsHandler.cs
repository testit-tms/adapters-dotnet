using System.Net;

namespace Tms.Adapter.Core.Client
{
    public class HttpVersionsHandler : DelegatingHandler
    {
        public HttpVersionsHandler(HttpMessageHandler innerHandler) : base(innerHandler)
        {
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Version = HttpVersion.Version20;
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }
}