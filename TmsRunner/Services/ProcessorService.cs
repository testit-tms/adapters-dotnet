using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

using Newtonsoft.Json;

using System.Text;
using System.Text.RegularExpressions;
using Tms.Adapter.Models;
using TmsRunner.Entities;
using TmsRunner.Entities.AutoTest;
using TmsRunner.Managers;
using TmsRunner.Utils;
using File = Tms.Adapter.Models.File;

namespace TmsRunner.Services;

public sealed class ProcessorService(ILogger<ProcessorService> logger,
                                     TmsManager apiClient,
                                     TmsSettings tmsSettings,
                                     LogParser parser)
{
    private async Task<List<Step>> GetStepsWithAttachmentsAsync(string? traceJson, List<Guid> attachmentIds)
    {
        var messages = LogParser.GetMessages(traceJson ?? string.Empty);

        var testCaseStepsHierarchical = new List<Step>();
        var stepsTable = new Dictionary<Guid, Step>();
        Step? parentStep = null;
        var nestingLevel = 1;

        foreach (var message in messages)
        {
            switch (message.Type)
            {
                case MessageType.TmsStep:
                    {
                        var step = JsonConvert.DeserializeObject<Step>(message.Value ?? string.Empty);
                        if (step == null)
                        {
                            logger.LogWarning("Can not deserialize step: {Step}", message.Value);
                            break;
                        }

                        stepsTable.Add(step.Guid, step);
                        
                        if ((step.CallerMethodType != null && parentStep == null) ||
                            (step.CurrentMethodType != null && parentStep == null))
                        {
                            step.NestingLevel = nestingLevel = 1;
                            testCaseStepsHierarchical.Add(step);
                            parentStep = step;
                            nestingLevel++;
                        }
                        else
                        {
                            var calledMethod = GetCalledMethod(step.CallerMethod);

                            while (parentStep != null && calledMethod != null && parentStep.CurrentMethod != calledMethod)
                            {
                                parentStep = parentStep.ParentStep;
                                nestingLevel--;
                            }

                            if (parentStep == null)
                            {
                                step.NestingLevel = nestingLevel = 1;
                                testCaseStepsHierarchical.Add(step);
                                parentStep = step;
                                nestingLevel++;
                            }
                            else
                            {
                                step.ParentStep = parentStep;
                                step.NestingLevel = nestingLevel;
                                parentStep.Steps.Add(step);
                                parentStep = step;
                                nestingLevel++;
                            }
                        }

                        break;
                    }
                case MessageType.TmsStepResult:
                    {
                        var stepResult = JsonConvert.DeserializeObject<StepResult>(message.Value ?? string.Empty);

                        if (stepResult == null)
                        {
                            logger.LogWarning("Can not deserialize step result: {StepResult}", message.Value ?? string.Empty);
                            break;
                        }

                        parentStep = MapStep(stepsTable, stepResult);

                        break;
                    }
                case MessageType.TmsStepAttachmentAsText:
                    {
                        var attachment = JsonConvert.DeserializeObject<File>(message.Value ?? string.Empty);
                        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(attachment!.Content));
                        var createdAttachment =
                            await apiClient.UploadAttachmentAsync(Path.GetFileName(attachment.Name), ms).ConfigureAwait(false);

                        if (parentStep is not null)
                        {
                            if (attachment.CallerMemberName == parentStep.CurrentMethod)
                            {
                                parentStep.Attachments.Add(createdAttachment.Id);
                            }
                        }
                        else
                        {
                            attachmentIds.Add(createdAttachment.Id);
                        }

                        break;
                    }
                case MessageType.TmsStepAttachment:
                    {
                        var file = JsonConvert.DeserializeObject<File>(message.Value ?? string.Empty);

                        if (System.IO.File.Exists(file!.PathToFile))
                        {
                            await using var fs = new FileStream(file.PathToFile, FileMode.Open, FileAccess.Read);
                            var attachment = await apiClient.UploadAttachmentAsync(Path.GetFileName(file.PathToFile), fs).ConfigureAwait(false);

                            if (parentStep is not null)
                            {
                                if (file.CallerMemberName == parentStep.CurrentMethod)
                                {
                                    parentStep.Attachments.Add(attachment.Id);
                                }
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

        return testCaseStepsHierarchical;
    }

    private static string? GetCalledMethod(string? calledMethod)
    {
        if (calledMethod == null || !calledMethod.Contains('<'))
        {
            return calledMethod;
        }

        const string pattern = "(?<=\\<)(.*)(?=\\>)";
        var regex = new Regex(pattern);
        var match = regex.Match(calledMethod);

        return match.Groups[1].Value;
    }

    public async Task ProcessAutoTestAsync(TestResult testResult)
    {
        var traceJson = GetTraceJson(testResult);
        var parameters = LogParser.GetParameters(traceJson);
        var autoTest = parser.GetAutoTest(testResult, parameters);
        autoTest.Message = LogParser.GetMessage(traceJson);

        var attachmentIds = new List<Guid>();
        var testCaseSteps = await GetStepsWithAttachmentsAsync(traceJson, attachmentIds).ConfigureAwait(false);

        autoTest.Setup = testCaseSteps
            .Where(x => x.CallerMethodType == CallerMethodType.Setup)
            .Select(AutoTestStep.ConvertFromStep)
            .ToList();

        autoTest.Steps = testCaseSteps
            .Where(x => x.CallerMethodType == CallerMethodType.TestMethod)
            .Select(AutoTestStep.ConvertFromStep)
            .ToList();

        autoTest.Teardown = testCaseSteps
            .Where(x => x.CallerMethodType == CallerMethodType.Teardown)
            .Select(AutoTestStep.ConvertFromStep)
            .ToList();


        var existAutotest = await apiClient.GetAutotestByExternalIdAsync(autoTest.ExternalId).ConfigureAwait(false);

        if (existAutotest == null)
        {
            existAutotest = await apiClient.CreateAutotestAsync(autoTest).ConfigureAwait(false);
        }
        else
        {
            autoTest.IsFlaky = existAutotest.IsFlaky;

            await apiClient.UpdateAutotestAsync(autoTest).ConfigureAwait(false);
        }

        if (autoTest.WorkItemIds.Count > 0)
        {
            await UpdateTestLinkToWorkItems(existAutotest.Id.ToString(), autoTest.WorkItemIds);
        }

        if (!string.IsNullOrEmpty(testResult.ErrorMessage))
        {
            autoTest.Message += Environment.NewLine + testResult.ErrorMessage;
        }

        var autoTestResultRequestBody = GetAutoTestResultsForTestRunModel(autoTest, testResult, traceJson,
            testCaseSteps, attachmentIds, parameters, tmsSettings.IgnoreParameters);

        await apiClient.SubmitResultToTestRunAsync(tmsSettings.TestRunId, autoTestResultRequestBody)
            .ConfigureAwait(false);
    }

    private async Task UpdateTestLinkToWorkItems(string autoTestId, List<string> workItemIds)
    {
        var linkedWorkItems = await apiClient.GetWorkItemsLinkedToAutoTestAsync(autoTestId);

        foreach (var linkedWorkItem in linkedWorkItems) {
            var linkedWorkItemId = linkedWorkItem.GlobalId.ToString();

            if (workItemIds.Contains(linkedWorkItemId)) {
                workItemIds.Remove(linkedWorkItemId);

                continue;
            }

            if (tmsSettings.AutomaticUpdationLinksToTestCases) {
                await apiClient.DeleteAutoTestLinkFromWorkItemAsync(autoTestId, linkedWorkItemId);
            }
        }

        await apiClient.LinkAutoTestToWorkItemAsync(autoTestId, workItemIds);
    }

    private static AutoTestResult GetAutoTestResultsForTestRunModel(AutoTest autoTest,
                                                                    TestResult testResult,
                                                                    string traceJson,
                                                                    IReadOnlyCollection<Step> testCaseSteps,
                                                                    List<Guid> attachmentIds,
                                                                    Dictionary<string, string>? parameters,
                                                                    bool isIgnoreParameters)
    {
        var stepResults =
            testCaseSteps
                .Where(x => x.CallerMethodType == CallerMethodType.TestMethod)
                .Select(AutoTestStepResult.ConvertFromStep).ToList();

        var setupResults =
            testCaseSteps
                .Where(x => x.CallerMethodType == CallerMethodType.Setup)
                .Select(AutoTestStepResult.ConvertFromStep).ToList();

        var teardownResults =
            testCaseSteps
                .Where(x => x.CallerMethodType == CallerMethodType.Teardown)
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
            Parameters = isIgnoreParameters ? new Dictionary<string, string>() : parameters!,
            Attachments = attachmentIds,
        };
        if (!string.IsNullOrEmpty(testResult.ErrorStackTrace))
        {
            autoTestResultRequestBody.Traces = testResult.ErrorStackTrace.TrimStart();
        }

        return autoTestResultRequestBody;
    }

    private static string GetTraceJson(TestResult testResult)
    {
        var debugTraceMessages = testResult.Messages.Select(x => x.Text);
        var traceJson = string.Join("\n", debugTraceMessages);

        return traceJson;
    }

    private static Step? MapStep(Dictionary<Guid, Step> stepsDictionary, StepResult stepResult)
    {
        if (!stepsDictionary.TryGetValue(stepResult.Guid, out var stepToUpdate))
        {
            return null;
        }

        stepToUpdate.CompletedOn = stepResult.CompletedOn;
        stepToUpdate.Duration = stepResult.Duration;
        stepToUpdate.Result = stepResult.Result;
        stepToUpdate.Outcome = stepResult.Outcome;

        return stepToUpdate.ParentStep;
    }
}