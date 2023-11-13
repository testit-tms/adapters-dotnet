using System.Collections.Concurrent;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Serilog;
using TmsRunner.Logger;
using TmsRunner.Services;

namespace TmsRunner.Handlers;

public class RunEventHandler : ITestRunEventsHandler2
{
    public readonly ConcurrentBag<TestResult> FailedTestResults;

    private readonly ConcurrentBag<Task> _uploadTasks;
    private readonly AutoResetEvent _waitHandle;
    private readonly bool _isLastRun;
    private readonly ILogger _logger;
    private readonly ProcessorService _processorService;

    public RunEventHandler(AutoResetEvent waitHandle, bool isLastRun, ProcessorService processorService)
    {
        _waitHandle = waitHandle;
        _isLastRun = isLastRun;
        _processorService = processorService;
        
        _logger = LoggerFactory.GetLogger().ForContext<RunEventHandler>();
        FailedTestResults = new ConcurrentBag<TestResult>();
        _uploadTasks = new ConcurrentBag<Task>();
    }

    public void HandleLogMessage(TestMessageLevel level, string? message)
    {
        _logger.Debug("Run Message: {Message}", message);
    }

    public void HandleTestRunComplete(
        TestRunCompleteEventArgs? testRunCompleteArgs,
        TestRunChangedEventArgs? lastChunkArgs,
        ICollection<AttachmentSet>? runContextAttachments,
        ICollection<string>? executorUris)
    {

        _uploadTasks.Add(ProcessNewTestResults(lastChunkArgs));
        Task.WaitAll(_uploadTasks.ToArray());
        _logger.Debug("Test Run completed");

        _waitHandle.Set();
    }

    public void HandleTestRunStatsChange(TestRunChangedEventArgs? testRunChangedArgs)
    {
        _uploadTasks.Add(ProcessNewTestResults(testRunChangedArgs));
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

    private async Task ProcessNewTestResults(TestRunChangedEventArgs? args)
    {
        if (args?.NewTestResults == null)
        {
            return;
        }

        var testResultsToUpload = new List<TestResult>();
        
        if (_isLastRun)
        {
            testResultsToUpload.AddRange(args.NewTestResults);
        }
        else
        {
            args.NewTestResults
                .Where(x => x.Outcome == TestOutcome.Failed)
                .ToList()
                .ForEach(FailedTestResults.Add);
            
            testResultsToUpload.AddRange(args.NewTestResults.Where(x => !FailedTestResults.Contains(x)));
        }
        
        await _processorService.TryUploadTestResults(testResultsToUpload);
    }
}