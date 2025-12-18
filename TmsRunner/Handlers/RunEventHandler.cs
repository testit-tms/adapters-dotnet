using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using TmsRunner.Services;
using System.Collections.Concurrent;

namespace TmsRunner.Handlers;

#pragma warning disable CA1711
public sealed class RunEventHandler(ILogger<RunEventHandler> logger, EventWaitHandle waitHandle,

    ProcessorService processorService) : ITestRunEventsHandler, IDisposable
{
    private readonly List<Task> _processTestResultsTasks = [];
    private readonly ConcurrentBag<TestCase> _failedTestCases = [];
    private bool _hasUploadErrors;

    public void HandleLogMessage(TestMessageLevel level, string? message)
    {
        logger.LogDebug("Run Message: {Message}", message);
    }

    public void HandleTestRunComplete(TestRunCompleteEventArgs testRunCompleteArgs,
                                      TestRunChangedEventArgs? lastChunkArgs,
                                      ICollection<AttachmentSet>? runContextAttachments,
                                      ICollection<string>? executorUris)
    {
        if (lastChunkArgs is { NewTestResults: not null })
        {
            _processTestResultsTasks.Add(ProcessTestResultsAsync(lastChunkArgs.NewTestResults));
        }

        logger.LogDebug("Test Run completed");
        _ = waitHandle.Set();
    }

    public void HandleTestRunStatsChange(TestRunChangedEventArgs? testRunChangedArgs)
    {
        if (testRunChangedArgs?.NewTestResults == null) return;

        foreach (var result in testRunChangedArgs.NewTestResults)
        {
            if (result?.Outcome == TestOutcome.Failed)
            {
                _failedTestCases.Add(result.TestCase);
            }
        }

        _processTestResultsTasks.Add(ProcessTestResultsAsync(testRunChangedArgs.NewTestResults));
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

    public static bool AttachDebuggerToProcess(int pid)
    {
        // No op
        return false;
    }

    public void WaitForEnd()
    {
        _ = waitHandle.WaitOne();
    }

    public List<Task> GetProcessTestResultsTasks()
    {
        return _processTestResultsTasks;
    }

    public IEnumerable<TestCase> GetFailedTestCases()
    {
        return _failedTestCases.ToArray();
    }

    public void ClearFailedTestCases()
    {
        _failedTestCases.Clear();
    }

    public bool HasUploadErrors => _hasUploadErrors;

    private async Task ProcessTestResultsAsync(IEnumerable<TestResult?>? testResults)
    {
        if (testResults == null)
        {
            return;
        }

        foreach (var testResult in testResults)
        {
            if (testResult == null)
            {
                continue;
            }
            
            logger.LogDebug("Start test '{Name}' upload", testResult.DisplayName);

            try
            {
                await processorService.ProcessAutoTestAsync(testResult).ConfigureAwait(false);
                
                logger.LogInformation("Success test '{Name}' upload", testResult.DisplayName);
            }
            catch (Exception e)
            {
                _hasUploadErrors = true;
                logger.LogError(e, "Failed test '{Name}' upload", testResult.DisplayName);
            }
        }
    }

    public void Dispose()
    {
        _processTestResultsTasks.Clear();
        _failedTestCases.Clear();
        waitHandle.Dispose();
    }
}
#pragma warning restore CA1711