using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Logging;
using SyncStorage.ApiClient.Api;
using SyncStorage.ApiClient.Client;
using SyncStorage.ApiClient.Model;

namespace Tms.Adapter.Core.SyncStorage;

/// <summary>
/// Sync Storage HTTP API via the OpenAPI-generated client + raw keep-alive.
/// </summary>
public sealed class SyncStorageClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly HttpClientHandler _handler;
    private readonly string _baseUrl;
    private readonly HealthApi _healthApi;
    private readonly WorkersApi _workersApi;
    private readonly TestResultsApi _testResultsApi;
    private readonly ILogger _logger;

    public SyncStorageClient(string baseUrl, ILogger logger)
    {
        _logger = logger;
        _baseUrl = baseUrl.TrimEnd('/');
        _handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };
        _httpClient = new HttpClient(_handler)
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        var configuration = new Configuration { BasePath = _baseUrl };
        _healthApi = new HealthApi(_httpClient, configuration, _handler);
        _workersApi = new WorkersApi(_httpClient, configuration, _handler);
        _testResultsApi = new TestResultsApi(_httpClient, configuration, _handler);
    }

    public async Task<bool> HealthCheckAsync()
    {
        try
        {
            await _healthApi.HealthGetAsync().ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<HealthStatusResponse?> GetHealthAsync()
    {
        try
        {
            return await _healthApi.HealthGetAsync().ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
    }

    public async Task<RegisterResponse> RegisterWorkerAsync(string pid, string testRunId)
    {
        _logger.LogDebug("Registering worker {Pid} for test run {TestRunId}", pid, testRunId);

        try
        {
            var request = new RegisterRequest(pid, testRunId);
            return await _workersApi.RegisterPostAsync(request).ConfigureAwait(false);
        }
        catch (ApiException ex) when (ex.ErrorCode == 409)
        {
            throw new InvalidOperationException(
                $"Sync-storage rejected register (HTTP 409): bound to another testRunId. " +
                $"Current testRunId: {testRunId}. Body: {ex.ErrorContent}",
                ex);
        }
    }

    public async Task<bool> SendInProgressTestResultAsync(string testRunId, TestResultCutApiModel model)
    {
        _logger.LogDebug("Sending in-progress test result to Sync Storage: {ExternalId}", model.AutoTestExternalId);

        try
        {
            await _testResultsApi.InProgressTestResultPostAsync(testRunId, model).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Sync Storage in_progress request failed");
            return false;
        }
    }

    public async Task SetWorkerStatusAsync(string pid, string status, string testRunId)
    {
        var request = new SetWorkerStatusRequest(pid, status, testRunId);
        _logger.LogDebug("Setting worker {Pid} status to {Status}", pid, status);
        await _workersApi.SetWorkerStatusPostAsync(request).ConfigureAwait(false);
    }

    /// <summary>
    /// Single non-retried keep-alive ping. Failures are logged and ignored.
    /// </summary>
    public async Task<bool> SendKeepAliveAsync(string pid, string testRunId)
    {
        if (string.IsNullOrEmpty(pid) || string.IsNullOrEmpty(testRunId))
            return false;

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/keep_alive");
            request.Content = new StringContent(
                $"{{\"pid\":\"{JsonEscape(pid)}\",\"testRunId\":\"{JsonEscape(testRunId)}\"}}",
                Encoding.UTF8,
                "application/json");

            using var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.OK)
                return true;

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                var errorBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (errorBody.Contains("Worker not found", StringComparison.OrdinalIgnoreCase))
                    _logger.LogWarning("keep_alive: worker not found (pid={Pid})", pid);
                else
                    _logger.LogDebug("keep_alive endpoint unavailable (HTTP 404, pid={Pid})", pid);
                return false;
            }

            _logger.LogDebug("keep_alive failed: HTTP {StatusCode} (pid={Pid})", (int)response.StatusCode, pid);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "keep_alive failed for pid={Pid}", pid);
            return false;
        }
    }

    private static string JsonEscape(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);

    public void Dispose()
    {
        _httpClient.Dispose();
        _handler.Dispose();
    }
}
