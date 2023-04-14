using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Serilog;
using TmsRunner.Logger;

namespace TmsRunner.Handlers;

public class DiscoveryEventHandler : ITestDiscoveryEventsHandler
{
    private AutoResetEvent waitHandle;
    private readonly ILogger _logger;
    public List<TestCase> DiscoveredTestCases { get; }

    public DiscoveryEventHandler(AutoResetEvent waitHandle)
    {
        this.waitHandle = waitHandle;
        DiscoveredTestCases = new List<TestCase>();
        _logger = LoggerFactory.GetLogger().ForContext<DiscoveryEventHandler>();
    }

    public void HandleDiscoveredTests(IEnumerable<TestCase> discoveredTestCases)
    {
        _logger.Debug("Discovery tests");

        if (discoveredTestCases == null) return;

        DiscoveredTestCases.AddRange(discoveredTestCases);

        _logger.Debug(
            "Added test cases: {@TestCases}", 
            discoveredTestCases.Select(t => t.FullyQualifiedName));
    }

    public void HandleDiscoveryComplete(long totalTests, IEnumerable<TestCase>? lastChunk, bool isAborted)
    {
        if (lastChunk != null)
        {
            DiscoveredTestCases.AddRange(lastChunk);
        }

        _logger.Debug("Discovery completed");

        waitHandle.Set();
    }

    public void HandleLogMessage(TestMessageLevel level, string message)
    {
        _logger.Debug("Discovery Message: {Message}", message);
    }

    public void HandleRawMessage(string rawMessage)
    {
        // No op
    }
}