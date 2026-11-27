using Microsoft.Extensions.Logging;

namespace Tms.Adapter.Core.SyncStorage;

/// <summary>
/// Background keep-alive pings for sync-storage while the worker stays in_progress.
/// </summary>
internal sealed class SyncStorageKeepAlive
{
    internal const int DefaultIntervalSec = 25;

    private readonly SyncStorageClient _client;
    private readonly string _pid;
    private readonly string _testRunId;
    private readonly int _intervalSec;
    private readonly ILogger _logger;

    private volatile bool _running;
    private Thread? _thread;

    public SyncStorageKeepAlive(
        SyncStorageClient client,
        string pid,
        string testRunId,
        ILogger logger,
        int intervalSec = DefaultIntervalSec)
    {
        _client = client;
        _pid = pid;
        _testRunId = testRunId;
        _logger = logger;
        _intervalSec = intervalSec > 0 ? intervalSec : DefaultIntervalSec;
    }

    public void Start()
    {
        if (_running)
            return;

        _running = true;
        _thread = new Thread(Loop)
        {
            IsBackground = true,
            Name = "sync-storage-keep-alive"
        };
        _thread.Start();
        _logger.LogDebug("SyncStorage keep-alive started (interval {Interval}s, pid={Pid})", _intervalSec, _pid);
    }

    public void Stop()
    {
        _running = false;
        _thread?.Interrupt();
        _thread = null;
        _logger.LogDebug("SyncStorage keep-alive stopped (pid={Pid})", _pid);
    }

    private void Loop()
    {
        while (_running)
        {
            try
            {
                _client.SendKeepAliveAsync(_pid, _testRunId).GetAwaiter().GetResult();
                Thread.Sleep(TimeSpan.FromSeconds(_intervalSec));
            }
            catch (ThreadInterruptedException)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "SyncStorage keep-alive loop error (pid={Pid})", _pid);
            }
        }
    }
}
