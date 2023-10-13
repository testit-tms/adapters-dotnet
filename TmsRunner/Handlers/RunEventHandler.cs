using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Serilog;
using System.Collections.Concurrent;
using TmsRunner.Logger;
using TmsRunner.Services;

namespace TmsRunner.Handlers;

public class RunEventHandler : ITestRunEventsHandler2
{
    private readonly AutoResetEvent _waitHandle;
    private readonly ILogger _logger;
    private readonly ProcessorService _processorService;

    public ConcurrentBag<TestResult> FailedTestResults;
    public volatile bool HasUploadErrors;

    public RunEventHandler(AutoResetEvent waitHandle, ProcessorService processorService)
    {
        FailedTestResults = new ConcurrentBag<TestResult>();
        HasUploadErrors = false;

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
        TestRunChangedEventArgs? lastChunkArgs,
        ICollection<AttachmentSet>? runContextAttachments,
        ICollection<string>? executorUris)
    {
        ProcessNewTestResults(lastChunkArgs);

        _logger.Debug("Test Run completed");

        _waitHandle.Set();
    }

    public void HandleTestRunStatsChange(TestRunChangedEventArgs? testRunChangedArgs)
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

    public void UploadFailedTestResultsAfterRetry()
    {
        UploadTestResults(FailedTestResults).GetAwaiter().GetResult();
    }

    private void ProcessNewTestResults(TestRunChangedEventArgs? args)
    {
        if (args?.NewTestResults == null)
        {
            return;
        }

        foreach (var failedResult in args.NewTestResults.Where(r => r.Outcome == TestOutcome.Failed))
        {
            FailedTestResults.Add(failedResult);
        }

        var notFailedResults = args.NewTestResults.Where(r => r.Outcome != TestOutcome.Failed).ToArray();
        UploadTestResults(notFailedResults).GetAwaiter().GetResult();
    }

    private async Task UploadTestResults(IReadOnlyCollection<TestResult> testResults)
    {
        if (!testResults.Any())
        {
            return;
        }

        _logger.Debug("Run Selected Test Result: {@Results}", testResults.Select(t => t.DisplayName));

        await Parallel.ForEachAsync(testResults, async (testResult, _) =>
        {
            try
            {
                _logger.Information("Uploading test {Name}", testResult.DisplayName);

                await _processorService.ProcessAutoTest(testResult);

                _logger.Information("Uploaded test {Name}", testResult.DisplayName);
            }
            catch (Exception e)
            {
                HasUploadErrors = true;

                _logger.Error(e, "Uploaded test {Name} is failed", testResult.DisplayName);
            }
        });
    }
}