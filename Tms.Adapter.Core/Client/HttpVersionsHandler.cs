using System.Net;

namespace Tms.Adapter.Core.Client
{
    public class HttpVersionsHandler : DelegatingHandler
    {
        private static readonly Version Version = HttpVersion.Version20;

        public HttpVersionsHandler(HttpMessageHandler innerHandler) : base(innerHandler)
        {
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Version = Version;
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }
}
