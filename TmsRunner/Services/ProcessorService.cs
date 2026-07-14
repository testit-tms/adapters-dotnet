using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

using Newtonsoft.Json;

using System.Text;
using Tms.Adapter.Models;
using TmsRunner.Entities;
using TmsRunner.Entities.AutoTest;
using TmsRunner.Managers;
using TmsRunner.Utils;
using AutoTest = TmsRunner.Entities.AutoTest.AutoTest;
using AutoTestStep = TmsRunner.Entities.AutoTest.AutoTestStep;
using AutoTestStepResult = TmsRunner.Entities.AutoTest.AutoTestStepResult;
using File = Tms.Adapter.Models.File;

namespace TmsRunner.Services;

public sealed partial class ProcessorService(ILogger<ProcessorService> logger,
                                     TmsManager apiClient,
                                     TmsSettings tmsSettings,
                                     SyncStorageSession syncStorageSession)
{
    private readonly List<TestResult> _bufferedTestResults = [];
    private int _firstTestInProgressHandled;
    private async Task<List<StepModel>> GetStepsWithAttachmentsAsync(string? traceJson, List<Guid> attachmentIds)
    {
        var messages = LogParser.GetMessages(traceJson ?? string.Empty);

        var stepTree = new StepTreeBuilder();

        foreach (var message in messages)
        {
            switch (message.Type)
            {
                case MessageType.TmsStep:
                    {
                        var step = TryDeserialize<StepModel>(message.Value);
                        if (step == null)
                        {
                            logger.LogWarning("Can not deserialize step: {Step}", message.Value);
                            break;
                        }

                        if (!stepTree.AddStep(step))
                        {
                            logger.LogWarning("Duplicate step Guid ignored: {Guid}", step.Guid);
                        }

                        break;
                    }
                case MessageType.TmsStepResult:
                    {
                        var stepResult = TryDeserialize<StepResult>(message.Value);

                        if (stepResult == null)
                        {
                            logger.LogWarning("Can not deserialize step result: {StepResult}", message.Value ?? string.Empty);
                            break;
                        }

                        if (!stepTree.ApplyResult(stepResult))
                        {
                            logger.LogWarning("Step result references unknown step: {Guid}", stepResult.Guid);
                        }

                        break;
                    }
                case MessageType.TmsStepAttachmentAsText:
                    {
                        var attachment = TryDeserialize<File>(message.Value);
                        if (attachment == null)
                        {
                            logger.LogWarning("Can not deserialize attachment: {Attachment}", message.Value ?? string.Empty);
                            break;
                        }
                        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(attachment.Content ?? ""));
                        var createdAttachment =
                            await apiClient.UploadAttachmentAsync(Path.GetFileName(attachment.Name), ms).ConfigureAwait(false);

                        var attachmentStep = stepTree.GetAttachmentStep(attachment.StepGuid);
                        if (attachmentStep is not null)
                        {
                            attachmentStep.Attachments.Add(createdAttachment.Id);
                        }
                        else
                        {
                            attachmentIds.Add(createdAttachment.Id);
                        }

                        break;
                    }
                case MessageType.TmsStepAttachment:
                    {
                        var file = TryDeserialize<File>(message.Value);
                        if (file == null)
                        {
                            logger.LogWarning("Can not deserialize attachment file: {File}", message.Value ?? string.Empty);
                            break;
                        }

                        if (!string.IsNullOrEmpty(file.PathToFile) && System.IO.File.Exists(file.PathToFile))
                        {
                            await using var fs = new FileStream(file.PathToFile, FileMode.Open, FileAccess.Read);
                            var attachment = await apiClient.UploadAttachmentAsync(Path.GetFileName(file.PathToFile), fs).ConfigureAwait(false);

                            var attachmentStep = stepTree.GetAttachmentStep(file.StepGuid);
                            if (attachmentStep is not null)
                            {
                                attachmentStep.Attachments.Add(attachment.Id);
                            }
                            else
                            {
                                attachmentIds.Add(attachment.Id);
                            }
                        }

                        break;
                    }
                default:
                    logger.LogDebug("Un support message type: {MessageType}", message.Type);
                    break;
            }
        }

        return stepTree.Build();
    }

    public async Task ProcessAutoTestAsync(TestResult testResult)
    {
        if (tmsSettings.ImportRealtime)
        {
            await PublishAutoTestAsync(testResult, allowInProgress: true).ConfigureAwait(false);
            return;
        }

        await TryPublishFirstTestInProgressAsync(testResult).ConfigureAwait(false);

        _bufferedTestResults.Add(testResult);
        logger.LogDebug("Buffered test result for bulk import: {Name}", testResult.DisplayName);
    }

    public async Task FlushBufferedTestResultsAsync()
    {
        if (tmsSettings.ImportRealtime)
        {
            return;
        }

        foreach (var testResult in _bufferedTestResults)
        {
            await PublishAutoTestAsync(testResult, allowInProgress: false).ConfigureAwait(false);
        }

        _bufferedTestResults.Clear();
    }

    private async Task<bool> TryPublishFirstTestInProgressAsync(
        TestResult testResult,
        AutoTestResult? autoTestResultRequestBody = null)
    {
        if (syncStorageSession.Runner is not { IsRunning: true, IsMaster: true } runner
            || string.IsNullOrWhiteSpace(tmsSettings.ProjectId))
        {
            return false;
        }

        if (Interlocked.CompareExchange(ref _firstTestInProgressHandled, 1, 0) != 0)
        {
            return false;
        }

        autoTestResultRequestBody ??= await BuildAutoTestResultRequestBodyAsync(testResult).ConfigureAwait(false);

        var cut = Tms.Adapter.Core.Client.Converter.ToTestResultCutApiModel(
            autoTestResultRequestBody.ExternalId ?? string.Empty,
            testResult.Outcome.ToString(),
            testResult.StartTime.UtcDateTime,
            tmsSettings.ProjectId);

        if (!await runner.SendInProgressTestResultAsync(cut).ConfigureAwait(false))
        {
            Interlocked.Exchange(ref _firstTestInProgressHandled, 0);
            return false;
        }

        await apiClient.SubmitResultToTestRunAsync(tmsSettings.TestRunId, autoTestResultRequestBody, true)
            .ConfigureAwait(false);
        return true;
    }

    private async Task PublishAutoTestAsync(TestResult testResult, bool allowInProgress)
    {
        var autoTestResultRequestBody = await BuildAutoTestResultRequestBodyAsync(testResult).ConfigureAwait(false);

        if (allowInProgress)
        {
            await TryPublishFirstTestInProgressAsync(testResult, autoTestResultRequestBody).ConfigureAwait(false);
        }

        await apiClient.SubmitResultToTestRunAsync(tmsSettings.TestRunId, autoTestResultRequestBody)
            .ConfigureAwait(false);
    }

    private async Task<AutoTestResult> BuildAutoTestResultRequestBodyAsync(TestResult testResult)
    {
        var traceJson = GetTraceJson(testResult);
        var parameters = LogParser.GetParameters(traceJson);
        var autoTest = LogParser.GetAutoTest(testResult, parameters);
        autoTest.Message = LogParser.GetMessage(traceJson);

        var attachmentIds = new List<Guid>();
        var testCaseSteps = await GetStepsWithAttachmentsAsync(traceJson, attachmentIds).ConfigureAwait(false);

        autoTest.Setup = testCaseSteps
            .Where(x => GetStepPhase(x) == CallerMethodType.Setup)
            .Select(AutoTestStep.ConvertFromStep)
            .ToList();

        autoTest.Steps = testCaseSteps
            .Where(x => GetStepPhase(x) == CallerMethodType.TestMethod || GetStepPhase(x) == null)
            .Select(AutoTestStep.ConvertFromStep)
            .ToList();

        autoTest.Teardown = testCaseSteps
            .Where(x => GetStepPhase(x) == CallerMethodType.Teardown)
            .Select(AutoTestStep.ConvertFromStep)
            .ToList();

        var existAutotestResult = await apiClient.GetAutotestByExternalIdAsync(autoTest.ExternalId).ConfigureAwait(false);
        if (existAutotestResult == null)
        {
            HtmlEscapeUtils.EscapeHtmlInObject(autoTest);
            existAutotestResult = await apiClient.CreateAutotestAsync(autoTest).ConfigureAwait(false);
        }
        else
        {
            autoTest.IsFlaky = existAutotestResult.IsFlaky;
            HtmlEscapeUtils.EscapeHtmlInObject(autoTest);
            await apiClient.UpdateAutotestAsync(autoTest).ConfigureAwait(false);
        }

        if (autoTest.WorkItemIds.Count > 0)
        {
            await UpdateTestLinkToWorkItems(existAutotestResult.Id.ToString(), autoTest.WorkItemIds).ConfigureAwait(false);
        }

        if (!string.IsNullOrEmpty(testResult.ErrorMessage))
        {
            autoTest.Message += Environment.NewLine + testResult.ErrorMessage;
        }

        var autoTestResultRequestBody = GetAutoTestResultsForTestRunModel(autoTest, testResult, traceJson,
            testCaseSteps, attachmentIds, parameters, tmsSettings.IgnoreParameters);

        HtmlEscapeUtils.EscapeHtmlInObject(autoTestResultRequestBody);
        return autoTestResultRequestBody;
    }

    private async Task UpdateTestLinkToWorkItems(string autoTestId, List<string?> workItemIds)
    {
        var linkedWorkItems = await apiClient.GetWorkItemsLinkedToAutoTestAsync(autoTestId).ConfigureAwait(false);

        foreach (var linkedWorkItem in linkedWorkItems) {
            var linkedWorkItemId = linkedWorkItem.GlobalId.ToString(CultureInfo.InvariantCulture);

            if (workItemIds.Remove(linkedWorkItemId)) {
                continue;
            }

            if (tmsSettings.AutomaticUpdationLinksToTestCases) {
                await apiClient.DeleteAutoTestLinkFromWorkItemAsync(autoTestId, linkedWorkItemId).ConfigureAwait(false);
            }
        }

        await apiClient.LinkAutoTestToWorkItemAsync(autoTestId, workItemIds).ConfigureAwait(false);
    }

    private static AutoTestResult GetAutoTestResultsForTestRunModel(AutoTest autoTest,
                                                                    TestResult testResult,
                                                                    string traceJson,
                                                                    IReadOnlyCollection<StepModel> testCaseSteps,
                                                                    List<Guid> attachmentIds,
                                                                    Dictionary<string, string>? parameters,
                                                                    bool isIgnoreParameters)
    {
        var stepResults =
            testCaseSteps
                .Where(x => GetStepPhase(x) == CallerMethodType.TestMethod || GetStepPhase(x) == null)
                .Select(AutoTestStepResult.ConvertFromStep).ToList();

        var setupResults =
            testCaseSteps
                .Where(x => GetStepPhase(x) == CallerMethodType.Setup)
                .Select(AutoTestStepResult.ConvertFromStep).ToList();

        var teardownResults =
            testCaseSteps
                .Where(x => GetStepPhase(x) == CallerMethodType.Teardown)
                .Select(AutoTestStepResult.ConvertFromStep).ToList();


        var autoTestResultRequestBody = new AutoTestResult
        {
            ExternalId = autoTest.ExternalId,
            Outcome = testResult.Outcome,
            StartedOn = testResult.StartTime.UtcDateTime,
            CompletedOn = testResult.EndTime.UtcDateTime,
            Duration = (long)testResult.Duration.TotalMilliseconds,
            StepResults = stepResults,
            SetupResults = setupResults,
            TeardownResults = teardownResults,
            Links = LogParser.GetLinks(traceJson),
            Message = autoTest.Message!.TrimStart(Environment.NewLine.ToCharArray()),
            Parameters = isIgnoreParameters ? [] : parameters!,
            Attachments = attachmentIds,
        };
        
        autoTestResultRequestBody.Traces = autoTestResultRequestBody.Message + "\n" + testResult.ErrorStackTrace?.TrimStart();

        return autoTestResultRequestBody;
    }

    private static string GetTraceJson(TestResult testResult)
    {
        var debugTraceMessages = testResult.Messages.Select(x => x.Text);
        var traceJson = string.Join("\n", debugTraceMessages);

        return traceJson;
    }

    private T? TryDeserialize<T>(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return default;
        }

        try
        {
            return JsonConvert.DeserializeObject<T>(value);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Can not deserialize {Type}: {Value}", typeof(T).Name, value);
            return default;
        }
    }

    private static CallerMethodType? GetStepPhase(StepModel step) => step.Phase ?? step.CallerMethodType;
}