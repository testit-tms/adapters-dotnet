namespace TmsRunner.Handlers
{
    public class RetryHandler : DelegatingHandler
    {
        private const int MaxRetries = 10;
        private const int PoolDelayInMillis = 100;

        public RetryHandler(HttpMessageHandler innerHandler): base(innerHandler) { }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var counter = 0;
            HttpResponseMessage? response = null;

            do
            {
                try
                {
                    counter++;

                    response?.Dispose();
                    response = await base.SendAsync(request, cancellationToken);

                    if (response.IsSuccessStatusCode)
                    {
                        break;
                    }

                    await Task.Delay(TimeSpan.FromMilliseconds(PoolDelayInMillis * counter), cancellationToken);
                }
                catch { }
            } while (counter <= MaxRetries);

            return response;
        }
    }
}
