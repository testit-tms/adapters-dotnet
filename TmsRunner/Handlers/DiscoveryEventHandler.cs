using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace TmsRunner.Handlers;

#pragma warning disable CA1711
public sealed class DiscoveryEventHandler(ILogger<DiscoveryEventHandler> logger,
    EventWaitHandle waitHandle) : ITestDiscoveryEventsHandler, IDisposable
{
    private readonly List<TestCase> _discoveredTestCases = [];

    public void HandleDiscoveredTests(IEnumerable<TestCase>? discoveredTestCases)
    {
        logger.LogDebug("Discovery tests");

        if (discoveredTestCases == null)
        {
            return;
        }

        var testCases = discoveredTestCases.ToArray();
        _discoveredTestCases.AddRange(testCases);

        logger.LogDebug("Added test cases: {@TestCases}", testCases.Select(t => t.FullyQualifiedName));
    }

    public void HandleDiscoveryComplete(long totalTests, IEnumerable<TestCase>? lastChunk, bool isAborted)
    {
        if (lastChunk != null)
        {
            _discoveredTestCases.AddRange(lastChunk);
        }

        logger.LogDebug("Discovery completed");

        _ = waitHandle.Set();
    }

    public void WaitForEnd()
    {
        _ = waitHandle.WaitOne();
    }

    public void HandleLogMessage(TestMessageLevel level, string? message)
    {
        logger.LogDebug("Discovery Message: {Message}", message);
    }

    public void HandleRawMessage(string rawMessage)
    {
        // No op
    }

    public List<TestCase> GetTestCases()
    {
        return _discoveredTestCases;
    }

    public void Dispose()
    {
        _discoveredTestCases.Clear();
        waitHandle.Dispose();
    }
}
#pragma warning restore CA1711