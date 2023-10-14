using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Serilog;
using System.Runtime.CompilerServices;
using TmsRunner.Logger;
using TmsRunner.Services;

namespace TmsRunner.Handlers;

public class RunEventHandler : ITestRunEventsHandler2
{
    private readonly AutoResetEvent _waitHandle;
    private readonly ILogger _logger;
    private readonly ProcessorService _processorService;

    public readonly List<TestResult> FailedTestResults;
    public bool HasUploadErrors;

    public RunEventHandler(AutoResetEvent waitHandle, ProcessorService processorService)
    {
        FailedTestResults = new List<TestResult>();
        HasUploadErrors = false;

        _waitHandle = waitHandle;
        _logger = LoggerFactory.GetLogger().ForContext<RunEventHandler>();
        _processorService = processorService;
    }

    public void HandleLogMessage(TestMessageLevel level, string? message)
    {
        _logger.Debug("Run Message: {Message}", message);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void HandleTestRunComplete(
        TestRunCompleteEventArgs testRunCompleteArgs,
        TestRunChangedEventArgs? lastChunkArgs,
        ICollection<AttachmentSet>? runContextAttachments,
        ICollection<string>? executorUris)
    {
        ProcessNewTestResults(lastChunkArgs).GetAwaiter().GetResult();
        _logger.Debug("Test Run completed");
        _waitHandle.Set();
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void HandleTestRunStatsChange(TestRunChangedEventArgs? testRunChangedArgs)
    {
        ProcessNewTestResults(testRunChangedArgs).GetAwaiter().GetResult();
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

    public async Task UploadFailedTestResults()
    {
        await UploadTestResults(FailedTestResults);
    }

    public async Task UploadTestResults(IReadOnlyCollection<TestResult> testResults)
    {
        if (!testResults.Any())
        {
            return;
        }

        _logger.Debug("Run Selected Test Result: {@Results}", testResults.Select(t => t.DisplayName));

        await Parallel.ForEachAsync(testResults, async (testResult, cancellationToken) =>
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

    private async Task ProcessNewTestResults(TestRunChangedEventArgs? args)
    {
        if (args?.NewTestResults == null)
        {
            return;
        }

        var failedTestResults = args.NewTestResults.Where(r => r.Outcome == TestOutcome.Failed).ToArray();
        FailedTestResults.AddRange(failedTestResults);

        var notFailedResults = args.NewTestResults.Where(r => r.Outcome != TestOutcome.Failed).ToArray();
        await UploadTestResults(notFailedResults);
    }
}