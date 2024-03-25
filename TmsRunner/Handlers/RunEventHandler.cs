using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using TmsRunner.Services;

namespace TmsRunner.Handlers;

public sealed class RunEventHandler(ILogger<RunEventHandler> logger, AutoResetEvent waitHandle,
                                    ProcessorService processorService) : ITestRunEventsHandler, IDisposable
{
    private readonly List<Task> _processTestResultsTasks = [];

    public void HandleLogMessage(TestMessageLevel level, string? message)
    {
        logger.LogDebug("Run Message: {Message}", message);
    }

    public void HandleTestRunComplete(TestRunCompleteEventArgs testRunCompleteArgs,
                                      TestRunChangedEventArgs? lastChunkArgs,
                                      ICollection<AttachmentSet>? runContextAttachments,
                                      ICollection<string>? executorUris)
    {
        if (lastChunkArgs != null && lastChunkArgs.NewTestResults != null)
        {
            _processTestResultsTasks.Add(ProcessTestResultsAsync(lastChunkArgs.NewTestResults));
        }

        logger.LogDebug("Test Run completed");
        _ = waitHandle.Set();
    }

    public void HandleTestRunStatsChange(TestRunChangedEventArgs? testRunChangedArgs)
    {
        if (testRunChangedArgs != null && testRunChangedArgs.NewTestResults != null)
        {
            _processTestResultsTasks.Add(ProcessTestResultsAsync(testRunChangedArgs.NewTestResults));
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

    public void WaitForEnd()
    {
        _ = waitHandle.WaitOne();
    }

    public List<Task> GetProcessTestResultsTasks()
    {
        return _processTestResultsTasks;
    }

    private async Task ProcessTestResultsAsync(IEnumerable<TestResult?>? testResults)
    {
        if (testResults == null || !testResults.Any())
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
                logger.LogError(e, "Failed test '{Name}' upload", testResult.DisplayName);
            }
        }
    }

    public void Dispose()
    {
        _processTestResultsTasks?.Clear();
        waitHandle?.Dispose();
    }
}