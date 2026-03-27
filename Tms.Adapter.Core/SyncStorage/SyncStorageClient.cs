using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Tms.Adapter.Core.SyncStorage;

/// <summary>
/// Simple HTTP client for interacting with the Sync Storage service.
/// Replaces the auto-generated client used in the Python adapter.
/// </summary>
public sealed class SyncStorageClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly string _baseUrl;

    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new SnakeCaseNamingStrategy()
        },
        NullValueHandling = NullValueHandling.Ignore
    };

    public SyncStorageClient(string baseUrl, ILogger logger)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _logger = logger;

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };
        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
    }

    /// <summary>
    /// Check if Sync Storage is healthy and running.
    /// </summary>
    public async Task<bool> HealthCheckAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/health").ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Register a worker with the Sync Storage service.
    /// </summary>
    /// <returns>Registration response indicating master status.</returns>
    public async Task<RegisterResponse> RegisterWorkerAsync(string pid, string testRunId)
    {
        var request = new RegisterRequest
        {
            Pid = pid,
            TestRunId = testRunId
        };

        var json = JsonConvert.SerializeObject(request, JsonSettings);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _logger.LogDebug("Registering worker {Pid} for test run {TestRunId}", pid, testRunId);

        var response = await _httpClient.PostAsync($"{_baseUrl}/register", content).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        var result = JsonConvert.DeserializeObject<RegisterResponse>(responseBody, JsonSettings);

        return result ?? new RegisterResponse();
    }

    /// <summary>
    /// Send an in-progress test result to Sync Storage.
    /// </summary>
    public async Task<bool> SendInProgressTestResultAsync(string testRunId, TestResultCutModel model)
    {
        var json = JsonConvert.SerializeObject(model, JsonSettings);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var url = $"{_baseUrl}/in_progress_test_result?testRunId={Uri.EscapeDataString(testRunId)}";

        _logger.LogDebug("Sending in-progress test result to Sync Storage: {ExternalId}", model.AutoTestExternalId);

        var response = await _httpClient.PostAsync(url, content).ConfigureAwait(false);
        return response.IsSuccessStatusCode;
    }

    /// <summary>
    /// Set the status of a worker (in_progress or completed).
    /// </summary>
    public async Task SetWorkerStatusAsync(string pid, string status, string testRunId)
    {
        var request = new SetWorkerStatusRequest
        {
            Pid = pid,
            Status = status,
            TestRunId = testRunId
        };

        var json = JsonConvert.SerializeObject(request, JsonSettings);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _logger.LogDebug("Setting worker {Pid} status to {Status}", pid, status);

        var response = await _httpClient.PostAsync($"{_baseUrl}/set_worker_status", content).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}

#region DTOs

public class RegisterRequest
{
    public string Pid { get; set; } = string.Empty;
    public string TestRunId { get; set; } = string.Empty;
}

public class RegisterResponse
{
    public bool IsMaster { get; set; }
}

public class SetWorkerStatusRequest
{
    public string Pid { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string TestRunId { get; set; } = string.Empty;
}

/// <summary>
/// Simplified test result model sent to Sync Storage.
/// Mirrors the Python TestResultCutApiModel.
/// </summary>
public class TestResultCutModel
{
    public string AutoTestExternalId { get; set; } = string.Empty;
    public string StatusCode { get; set; } = string.Empty;
    public string? StartedOn { get; set; }
}

#endregion
