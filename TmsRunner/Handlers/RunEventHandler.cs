using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Serilog;
using TmsRunner.Logger;

namespace TmsRunner.Handlers;

public class RunEventHandler : ITestRunEventsHandler2
{
    private AutoResetEvent waitHandle;
    private readonly ILogger _logger;

    public List<TestResult> TestResults { get;}

    public RunEventHandler(AutoResetEvent waitHandle)
    {
        this.waitHandle = waitHandle;
        TestResults = new List<TestResult>();

        _logger = LoggerFactory.GetLogger().ForContext<RunEventHandler>();
    }

    public void HandleLogMessage(TestMessageLevel level, string message)
    {
        _logger.Debug("Run Message: {Message}", message);
    }

    public void HandleTestRunComplete(
        TestRunCompleteEventArgs testRunCompleteArgs,
        TestRunChangedEventArgs lastChunkArgs,
        ICollection<AttachmentSet> runContextAttachments,
        ICollection<string> executorUris)
    {
        if (lastChunkArgs != null && lastChunkArgs.NewTestResults != null)
        {
            TestResults.AddRange(lastChunkArgs.NewTestResults);
        }

        _logger.Debug("Test Run completed");

        waitHandle.Set();
    }

    public void HandleTestRunStatsChange(TestRunChangedEventArgs testRunChangedArgs)
    {
        if (testRunChangedArgs != null && testRunChangedArgs.NewTestResults != null)
        {
            TestResults.AddRange(testRunChangedArgs.NewTestResults);
        }
    }

    public void HandleRawMessage(string rawMessage)
    {
        // No op
    }

    public int LaunchProcessWithDebuggerAttached(TestProcessStartInfo testProcessStartInfo)
    {
        // No op
        return -1;
    }

    public bool AttachDebuggerToProcess(int pid)
    {
        // No op
        return false;
    }
}