using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using SyncStorage.ApiClient.Model;
using Tms.Adapter.Core.Client;

namespace Tms.Adapter.Core.SyncStorage;

/// <summary>
/// Manages the Sync Storage process lifecycle and worker coordination.
/// Port of the Python SyncStorageRunner to .NET.
/// </summary>
public sealed class SyncStorageRunner : IDisposable
{
    private const string SyncStorageVersion = "v0.2.0-tms-5.7";

    private const string SyncStorageRepoUrl =
        "https://github.com/testit-tms/sync-storage-public/releases/download/";

    private const int StartupTimeoutSeconds = 30;
    private const int DefaultPort = 49152;

    private readonly ILogger _logger;
    private readonly string _testRunId;
    private readonly int _port;
    private readonly string? _baseUrl;
    private readonly string? _privateToken;
    private readonly string _workerPid;

    private Process? _syncStorageProcess;
    private SyncStorageClient? _client;

    private bool _isRunning;
    private bool _isExternal;
    private bool _isMaster;
    // 0 -> not sent, 1 -> sent/reserved; must be atomic because xUnit can run tests in parallel.
    private int _inProgressSent;

    public bool IsRunning => _isRunning;
    public bool IsMaster => _isMaster;
    public bool IsAlreadyInProgress => _inProgressSent == 1;

    public SyncStorageRunner(
        string testRunId,
        int port,
        string? baseUrl,
        string? privateToken,
        ILogger logger)
    {
        _testRunId = testRunId;
        _port = port > 0 ? port : DefaultPort;
        _baseUrl = baseUrl;
        _privateToken = privateToken;
        _logger = logger;
        _workerPid = $"worker-{Environment.CurrentManagedThreadId}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
    }

    /// <summary>
    /// Start the Sync Storage service and register this worker.
    /// </summary>
    public async Task<bool> StartAsync()
    {
        if (_isRunning)
        {
            _logger.LogInformation("SyncStorage is already running");
            return true;
        }

        try
        {
            _client = new SyncStorageClient($"http://localhost:{_port}", _logger);

            // Check if already running externally
            if (await _client.HealthCheckAsync().ConfigureAwait(false))
            {
                _logger.LogInformation("SyncStorage already running on port {Port}, connecting to existing", _port);
                _isRunning = true;
                _isExternal = true;
                await RegisterWorkerAsync().ConfigureAwait(false);
                return true;
            }

            // Download and start the process
            var executablePath = await PrepareExecutableAsync().ConfigureAwait(false);
            StartProcess(executablePath);

            // Wait for startup
            if (!await WaitForStartupAsync(StartupTimeoutSeconds).ConfigureAwait(false))
            {
                _logger.LogError("SyncStorage failed to start within {Timeout}s", StartupTimeoutSeconds);
                return false;
            }

            _isRunning = true;
            _logger.LogInformation("SyncStorage started on port {Port}", _port);

            // Small delay as in Python/Java implementations
            await Task.Delay(2000).ConfigureAwait(false);

            await RegisterWorkerAsync().ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start SyncStorage");
            return false;
        }
    }

    /// <summary>
    /// Send an in-progress test result to Sync Storage (master only).
    /// </summary>
    public async Task<bool> SendInProgressTestResultAsync(TestResultCutApiModel model)
    {
        if (!_isMaster)
        {
            _logger.LogDebug("Not master worker, skipping send to SyncStorage");
            return false;
        }

        // Reserve a single "in_progress" slot across threads/process messages.
        // If the send fails, we release the reservation to allow retry on a later test.
        if (Interlocked.CompareExchange(ref _inProgressSent, 1, 0) != 0)
        {
            _logger.LogDebug("Test already in progress, skipping duplicate send");
            return false;
        }

        if (_client == null)
        {
            _logger.LogError("SyncStorageClient not initialized");
            Interlocked.Exchange(ref _inProgressSent, 0);
            return false;
        }

        try
        {
            var success = await _client.SendInProgressTestResultAsync(_testRunId, model).ConfigureAwait(false);

            if (!success)
            {
                Interlocked.Exchange(ref _inProgressSent, 0);
                return false;
            }

            _logger.LogDebug("Successfully sent in-progress test result to SyncStorage");
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send test result to SyncStorage");
            Interlocked.Exchange(ref _inProgressSent, 0);
            return false;
        }
    }

    /// <summary>
    /// Set the already-in-progress flag.
    /// </summary>
    public void SetIsAlreadyInProgress(bool value)
    {
        Interlocked.Exchange(ref _inProgressSent, value ? 1 : 0);
    }

    /// <summary>
    /// Set worker status (in_progress or completed).
    /// </summary>
    public async Task SetWorkerStatusAsync(string status)
    {
        if (_client == null)
        {
            _logger.LogError("SyncStorageClient not initialized");
            return;
        }

        try
        {
            await _client.SetWorkerStatusAsync(_workerPid, status, _testRunId).ConfigureAwait(false);
            _logger.LogInformation("Worker {Pid} status set to {Status}", _workerPid, status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set worker status to {Status}", status);
        }
    }

    /// <summary>
    /// Convert a TestContainer to a cut model for SyncStorage.
    /// </summary>
    public static TestResultCutApiModel ToTestResultCutModel(Models.TestContainer testContainer, string projectId)
    {
        return Converter.ToTestResultCutApiModel(testContainer, projectId);
    }

    #region Private methods

    private async Task RegisterWorkerAsync()
    {
        if (_client == null) return;

        try
        {
            var response = await _client.RegisterWorkerAsync(_workerPid, _testRunId).ConfigureAwait(false);
            _isMaster = response.IsMaster;

            _logger.LogInformation(
                _isMaster
                    ? "Registered as MASTER worker, PID: {Pid}"
                    : "Registered as worker, PID: {Pid}",
                _workerPid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register worker");
        }
    }

    private async Task<string> PrepareExecutableAsync()
    {
        var fileName = GetExecutableFileName();
        var cachesDir = Path.Combine(Directory.GetCurrentDirectory(), "build", ".caches");
        Directory.CreateDirectory(cachesDir);

        var targetPath = Path.Combine(cachesDir, fileName);

        if (File.Exists(targetPath))
        {
            _logger.LogInformation("Using cached SyncStorage executable: {Path}", targetPath);
            EnsureExecutable(targetPath);
            return targetPath;
        }

        var downloadUrl = $"{SyncStorageRepoUrl}{SyncStorageVersion}/{fileName}";
        _logger.LogInformation("Downloading SyncStorage from {Url}", downloadUrl);

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "TestIT-DotNet-Adapter");

        var bytes = await httpClient.GetByteArrayAsync(downloadUrl).ConfigureAwait(false);
        await File.WriteAllBytesAsync(targetPath, bytes).ConfigureAwait(false);

        _logger.LogInformation("Downloaded SyncStorage to {Path}", targetPath);
        EnsureExecutable(targetPath);

        return targetPath;
    }

    private void StartProcess(string executablePath)
    {
        var args = new List<string>();

        if (!string.IsNullOrEmpty(_testRunId))
        {
            args.Add("--testRunId");
            args.Add(_testRunId);
        }

        args.Add("--port");
        args.Add(_port.ToString());

        if (!string.IsNullOrEmpty(_baseUrl))
        {
            args.Add("--baseURL");
            args.Add(_baseUrl);
        }

        if (!string.IsNullOrEmpty(_privateToken))
        {
            args.Add("--privateToken");
            args.Add(_privateToken);
        }

        _logger.LogInformation("Starting SyncStorage: {Exe} {Args}", executablePath, string.Join(" ", args));

        var psi = new ProcessStartInfo
        {
            FileName = executablePath,
            Arguments = string.Join(" ", args),
            WorkingDirectory = Path.GetDirectoryName(executablePath),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        _syncStorageProcess = Process.Start(psi);

        if (_syncStorageProcess != null)
        {
            // Read output in background
            Task.Run(() => ReadProcessOutput(_syncStorageProcess));
        }
    }

    private async Task ReadProcessOutput(Process process)
    {
        try
        {
            while (!process.HasExited)
            {
                var line = await process.StandardOutput.ReadLineAsync().ConfigureAwait(false);
                if (line != null)
                {
                    _logger.LogInformation("SyncStorage: {Line}", line);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error reading SyncStorage output");
        }
    }

    private async Task<bool> WaitForStartupAsync(int timeoutSeconds)
    {
        if (_client == null) return false;

        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);

        while (DateTime.UtcNow < deadline)
        {
            if (await _client.HealthCheckAsync().ConfigureAwait(false))
            {
                return true;
            }

            await Task.Delay(1000).ConfigureAwait(false);
        }

        return false;
    }

    private static string GetExecutableFileName()
    {
        string osPart;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            osPart = "windows";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            osPart = "darwin";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            osPart = "linux";
        else
            throw new PlatformNotSupportedException($"Unsupported OS: {RuntimeInformation.OSDescription}");

        var arch = RuntimeInformation.OSArchitecture;
        var archPart = arch switch
        {
            Architecture.X64 => "amd64",
            Architecture.Arm64 => "arm64",
            _ => throw new PlatformNotSupportedException($"Unsupported architecture: {arch}")
        };

        var fileName = $"syncstorage-{SyncStorageVersion}-{osPart}_{archPart}";
        if (osPart == "windows")
            fileName += ".exe";

        return fileName;
    }

    private static void EnsureExecutable(string path)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                Process.Start("chmod", $"+x {path}")?.WaitForExit();
            }
            catch
            {
                // Best effort
            }
        }
    }

    #endregion

    public void Dispose()
    {
        _client?.Dispose();

        if (_syncStorageProcess != null && !_syncStorageProcess.HasExited)
        {
            try
            {
                _syncStorageProcess.Kill();
                _syncStorageProcess.Dispose();
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }
}
