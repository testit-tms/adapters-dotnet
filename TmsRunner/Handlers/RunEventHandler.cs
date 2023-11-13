using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Serilog;
using TmsRunner.Logger;
using TmsRunner.Services;

namespace TmsRunner.Handlers;

public class RunEventHandler : ITestRunEventsHandler2
{
    public readonly HashSet<TestResult> FailedTestResults;
    private readonly AutoResetEvent _waitHandle;
    private readonly bool _isLastRun;
    private readonly ILogger _logger;
    private readonly ProcessorService _processorService;
    private HashSet<Task> _uploadTasks;

    public RunEventHandler(AutoResetEvent waitHandle, bool isLastRun, ProcessorService processorService)
    {
        _waitHandle = waitHandle;
        _isLastRun = isLastRun;
        _processorService = processorService;
        
        _logger = LoggerFactory.GetLogger().ForContext<RunEventHandler>();
        _uploadTasks = new HashSet<Task>();
        FailedTestResults = new HashSet<TestResult>();
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

        _uploadTasks = new HashSet<Task>(_uploadTasks.Where(t => !t.IsCompleted))
        {
            ProcessNewTestResults(lastChunkArgs)
        };

        Task.WaitAll(_uploadTasks.ToArray());
        _logger.Debug("Test Run completed");

        _waitHandle.Set();
    }

    public void HandleTestRunStatsChange(TestRunChangedEventArgs? testRunChangedArgs)
    {
        lock (_uploadTasks)
        {
            _uploadTasks = new HashSet<Task>(_uploadTasks.Where(t => !t.IsCompleted))
        {
            ProcessNewTestResults(testRunChangedArgs)
        };
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
                .ForEach(r =>
                {
                    lock (FailedTestResults)
                    {
                        FailedTestResults.Add(r);
                    }
                });
            
            testResultsToUpload.AddRange(args.NewTestResults.Where(x => !FailedTestResults.Contains(x)));
        }
        
        await _processorService.TryUploadTestResults(testResultsToUpload);
    }
}