using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Serilog;
using TmsRunner.Logger;
using TmsRunner.Services;

namespace TmsRunner.Handlers;

public class RunEventHandler : ITestRunEventsHandler2
{
    public readonly List<TestResult> FailedTestResults;
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
        FailedTestResults = new List<TestResult>();
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

        ProcessNewTestResults(lastChunkArgs).Wait();
        _logger.Debug("Test Run completed");

        _waitHandle.Set();
    }

    public void HandleTestRunStatsChange(TestRunChangedEventArgs? testRunChangedArgs)
    {
        ProcessNewTestResults(testRunChangedArgs).Wait();
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
        
        if (_isLastRun)
        {
            await _processorService.TryUploadTestResults(args.NewTestResults);
        }
        else
        {
            args.NewTestResults
                .Where(x => x.Outcome == TestOutcome.Failed)
                .ToList()
                .ForEach(r => FailedTestResults.Add(r));
            
            await _processorService.TryUploadTestResults(args.NewTestResults.Where(x => !FailedTestResults.Contains(x)));
        }
    }
}