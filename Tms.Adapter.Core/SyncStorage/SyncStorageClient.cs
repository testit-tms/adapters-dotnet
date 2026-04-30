using System.Net.Http;
using Microsoft.Extensions.Logging;
using SyncStorage.ApiClient.Api;
using SyncStorage.ApiClient.Client;
using SyncStorage.ApiClient.Model;

namespace Tms.Adapter.Core.SyncStorage;

/// <summary>
/// Sync Storage HTTP API via the OpenAPI-generated client.
/// </summary>
public sealed class SyncStorageClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly HttpClientHandler _handler;
    private readonly HealthApi _healthApi;
    private readonly WorkersApi _workersApi;
    private readonly TestResultsApi _testResultsApi;
    private readonly ILogger _logger;

    public SyncStorageClient(string baseUrl, ILogger logger)
    {
        _logger = logger;
        var trimmed = baseUrl.TrimEnd('/');
        _handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };
        _httpClient = new HttpClient(_handler)
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        var configuration = new Configuration { BasePath = trimmed };
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

    public async Task<RegisterResponse> RegisterWorkerAsync(string pid, string testRunId)
    {
        _logger.LogDebug("Registering worker {Pid} for test run {TestRunId}", pid, testRunId);

        var request = new RegisterRequest(pid, testRunId);
        return await _workersApi.RegisterPostAsync(request).ConfigureAwait(false);
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

    public void Dispose()
    {
        _httpClient.Dispose();
        _handler.Dispose();
    }
}
