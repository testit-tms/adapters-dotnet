using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Serilog;
using TmsRunner.Logger;
using TmsRunner.Services;

namespace TmsRunner.Handlers;

public class RunEventHandler : ITestRunEventsHandler2
{
    public bool IsUploadError;
    public readonly List<TestResult> FailedTestResults;
    
    private readonly AutoResetEvent _waitHandle;
    private readonly ILogger _logger;
    private readonly ProcessorService _processorService;

    public RunEventHandler(AutoResetEvent waitHandle, ProcessorService processorService)
    {
        IsUploadError = false;
        FailedTestResults = new List<TestResult>();

        _waitHandle = waitHandle;
        _logger = LoggerFactory.GetLogger().ForContext<RunEventHandler>();
        _processorService = processorService;
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
        ProcessNewTestResults(lastChunkArgs);

        _logger.Debug("Test Run completed");

        _waitHandle.Set();
    }

    public void HandleTestRunStatsChange(TestRunChangedEventArgs testRunChangedArgs)
    {
        ProcessNewTestResults(testRunChangedArgs);
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

    private void ProcessNewTestResults(TestRunChangedEventArgs? args)
    {
        if (args?.NewTestResults == null)
        {
            return;
        }

        var failedTestResults = args.NewTestResults.Where(x => x.Outcome == TestOutcome.Failed).ToList();
        FailedTestResults.AddRange(failedTestResults);

        var testResultsToUpload = args.NewTestResults.Where(x => !FailedTestResults.Contains(x)).ToList();

        foreach (var testResult in testResultsToUpload)
        {
            _logger.Information("Uploading test {Name}", testResult.DisplayName);

            try
            {
                _processorService.ProcessAutoTest(testResult).GetAwaiter().GetResult();
                _logger.Information("Uploaded test {Name}", testResult.DisplayName);
            }
            catch (Exception e)
            {
                IsUploadError = true;
                _logger.Error(e, "Uploaded test {Name} is failed", testResult.DisplayName);
            }
        }
    }
}