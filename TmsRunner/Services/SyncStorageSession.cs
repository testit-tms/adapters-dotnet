using Microsoft.Extensions.Logging;
using Tms.Adapter.Core.SyncStorage;
using TmsRunner.Entities;

namespace TmsRunner.Services;

/// <summary>
/// Holds the optional Sync Storage runner for the current process (TmsRunner).
/// </summary>
public sealed class SyncStorageSession
{
    public SyncStorageRunner? Runner { get; private set; }

    public async Task TryStartAsync(TmsSettings settings, ILoggerFactory loggerFactory)
    {
        if (string.IsNullOrEmpty(settings.TestRunId))
        {
            return;
        }

        var runner = new SyncStorageRunner(
            testRunId: settings.TestRunId,
            port: settings.SyncStoragePort,
            baseUrl: settings.Url,
            privateToken: settings.PrivateToken,
            logger: loggerFactory.CreateLogger<SyncStorageRunner>());

        if (!await runner.StartAsync().ConfigureAwait(false))
        {
            runner.Dispose();
            return;
        }

        Runner = runner;
        await runner.SetWorkerStatusAsync("in_progress").ConfigureAwait(false);
    }

    public async Task ShutdownAsync()
    {
        if (Runner == null)
        {
            return;
        }

        try
        {
            await Runner.SetWorkerStatusAsync("completed").ConfigureAwait(false);
        }
        catch
        {
            // Best effort
        }

        Runner.Dispose();
        Runner = null;
    }
}
