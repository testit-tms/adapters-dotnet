using System.Net.Http;
using System.Text;
using TestIT.AdaptersApi.Client;
using Tms.Adapter.Core.Configurator;

namespace Tms.Adapter.Core.Client;

public static class AdaptersApiConfiguration
{
    private const string TraceHttpEnvVar = "TMS_TRACE_HTTP";

    public static Configuration Create(TmsSettings settings, out HttpClient httpClient)
    {
        var basePath = NormalizeBaseUrl(settings.Url);
        var cfg = new Configuration { BasePath = basePath };
        cfg.AddApiKeyPrefix("Authorization", "PrivateToken");
        cfg.AddApiKey("Authorization", settings.PrivateToken ?? string.Empty);

        var handler = CreateHandler(settings);
        httpClient = new HttpClient(handler, disposeHandler: true);

        if (IsTraceEnabled())
        {
            Console.WriteLine($"[TMS] Adapters API BasePath={basePath}");
            Console.WriteLine($"[TMS] Example GET {basePath}/api/adapters/testRuns/{{id}}");
        }

        return cfg;
    }

    public static void ApplyExceptionFactory(IApiAccessor api) =>
        api.ExceptionFactory = DetailedExceptionFactory;

    public static HttpMessageHandler CreateHandler(TmsSettings settings)
    {
        var inner = new HttpClientHandler();
        if (!settings.CertValidation)
            inner.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;

        return IsTraceEnabled() ? new TracingHandler(inner) : inner;
    }

    /// <summary>
    /// OpenAPI paths are /api/adapters/...; BasePath must be TMS host root only.
    /// </summary>
    public static string NormalizeBaseUrl(string? tmsUrl)
    {
        var url = (tmsUrl ?? string.Empty).TrimEnd('/');
        if (url.EndsWith("/api/adapters", StringComparison.OrdinalIgnoreCase))
            url = url[..^13];
        else if (url.EndsWith("/api/v2", StringComparison.OrdinalIgnoreCase))
            url = url[..^7];
        else if (url.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
            url = url[..^4];

        return url;
    }

    public static bool IsTraceEnabled() =>
        string.Equals(Environment.GetEnvironmentVariable(TraceHttpEnvVar), "true", StringComparison.OrdinalIgnoreCase);

    private static Exception? DetailedExceptionFactory(string method, IApiResponse response)
    {
        var status = (int)response.StatusCode;
        if (status < 400) return null;

        var body = string.IsNullOrWhiteSpace(response.RawContent) ? "<empty>" : response.RawContent;
        return new ApiException(status, $"Error calling {method}: HTTP {status}, body: {body}", response.RawContent, response.Headers);
    }

    private sealed class TracingHandler : DelegatingHandler
    {
        public TracingHandler(HttpMessageHandler inner) : base(inner) { }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Console.WriteLine($"[TMS HTTP] >>> {request.Method} {request.RequestUri}");
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var bytes = response.Content == null
                ? Array.Empty<byte>()
                : await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

            var preview = bytes.Length == 0
                ? "<empty>"
                : Encoding.UTF8.GetString(bytes, 0, Math.Min(bytes.Length, 300));
            if (bytes.Length > 300) preview += "...";

            Console.WriteLine($"[TMS HTTP] <<< {(int)response.StatusCode} {request.RequestUri} body={preview}");

            if (response.Content != null)
            {
                var content = new ByteArrayContent(bytes);
                foreach (var header in response.Content.Headers)
                    content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                response.Content = content;
            }

            return response;
        }
    }
}
