using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AutoMapper;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Serilog;
using Tms.Adapter.Models;
using TmsRunner.Client;
using TmsRunner.Extensions;
using TmsRunner.Logger;
using TmsRunner.Mapper;
using TmsRunner.Models;
using TmsRunner.Utils;
using File = Tms.Adapter.Models.File;

namespace TmsRunner.Services
{
    public class ProcessorService
    {
        private readonly ITmsClient _apiClient;
        private readonly TmsSettings _tmsSettings;
        private readonly LogParser _parser;
        private readonly IMapper _mapper;
        private readonly Dictionary<string, List<Step>> _autoTestSteps = new();

        private readonly ILogger _logger = LoggerFactory.GetLogger().ForContext<ProcessorService>();

        public ProcessorService(
            ITmsClient apiClient,
            TmsSettings tmsSettings,
            LogParser parser)
        {
            _apiClient = apiClient;
            _tmsSettings = tmsSettings;
            _parser = parser;
            _mapper = MapperFactory.ConfigureMapper();
        }

        private async Task<(List<Step> AutoTestSteps, List<Step> TestCaseStep)> GetStepsWithAttachments(
            string? traceJson,
            string methodName, string className, ICollection<Guid> attachmentIds)
        {
            var messages = _parser.GetMessages(traceJson);

            var testCaseStepsHierarchical = new List<Step>();
            Step? parentStep = null;
            var nestingLevel = 1;

            foreach (var message in messages)
            {
                switch (message.Type)
                {
                    case MessageType.TmsStep:
                    {
                        var step = JsonSerializer.Deserialize<Step>(message.Value);
                        if (step == null)
                        {
                            _logger.Warning("Can not deserialize step: {Step}", message.Value);
                            break;
                        }

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

                            while (parentStep != null && calledMethod != null &&
                                   parentStep?.CurrentMethod != calledMethod)
                            {
                                parentStep = parentStep?.ParentStep;
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
                        var stepResult = JsonSerializer.Deserialize<StepResult>(message.Value);

                        if (stepResult == null)
                        {
                            _logger.Warning("Can not deserialize step result: {StepResult}", message.Value);
                            break;
                        }

                        parentStep = MapStep(parentStep, stepResult);

                        if (parentStep != null)
                        {
                            parentStep = parentStep.ParentStep ?? null;
                        }

                        break;
                    }
                    case MessageType.TmsStepAttachmentAsText:
                    {
                        var attachment = JsonSerializer.Deserialize<File>(message.Value);
                        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(attachment!.Content));
                        var createdAttachment =
                            await _apiClient.UploadAttachment(Path.GetFileName(attachment.Name), ms);

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
                        var file = JsonSerializer.Deserialize<File>(message.Value);

                        if (System.IO.File.Exists(file!.PathToFile))
                        {
                            await using var fs = new FileStream(file.PathToFile, FileMode.Open, FileAccess.Read);
                            var attachment = await _apiClient.UploadAttachment(Path.GetFileName(file.PathToFile), fs);

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
                        _logger.Debug("Un support message type: {MessageType}", message.Type);
                        break;
                }
            }

            var testCaseSteps = testCaseStepsHierarchical.Flatten(x => x.Steps)
                .OrderBy(x => x.NestingLevel)
                .ToList();
            _autoTestSteps.TryGetValue($"{className}:{methodName}", out var autoTestSteps);

            if (autoTestSteps == null)
            {
                _autoTestSteps.Add($"{className}:{methodName}", testCaseStepsHierarchical);
                autoTestSteps = testCaseStepsHierarchical;
            }
            else
            {
                _logger.Debug("Flatten");
                
                var flattenAutoTestSteps = autoTestSteps.Flatten(x => x.Steps).ToList();
            
                foreach (var step in testCaseSteps)
                {
                    var existedSteps = flattenAutoTestSteps
                        .Where(x => x.StackTrace() == step.StackTrace())
                        .ToList();
                    var newSteps = testCaseSteps
                        .Where(x => x.StackTrace() == step.StackTrace())
                        .ToList();
                    var diff = existedSteps.Count - newSteps.Count;
            
                    if (diff >= 0) continue;
            
                    if (step.CallerMethod == methodName)
                    {
                        var newStep = _mapper.Map<Step>(step);
                        autoTestSteps.Add(newStep);
                        flattenAutoTestSteps.Add(newStep);
                    }
                    else
                    {
                        var parentStackTrace = step.StackTrace()
                            .Remove(step.StackTrace().LastIndexOf(Environment.NewLine));
                        var parentSteps = flattenAutoTestSteps.Where(x => x.StackTrace() == parentStackTrace)
                            .ToList();
                        var i = 0;
                        do
                        {
                            i++;
                            foreach (var parent in parentSteps)
                            {
                                var newStep = _mapper.Map<Step>(step);
                                parent.Steps.Add(newStep);
                                flattenAutoTestSteps.Add(newStep);
                            }
                        } while (parentSteps.Count * i + diff < 0);
                    }
                }
            }

            return (autoTestSteps, testCaseStepsHierarchical);
        }

        private static string? GetCalledMethod(string? calledMethod)
        {
            if (calledMethod == null || !calledMethod.Contains("<")) return calledMethod;

            const string pattern = "(?<=\\<)(.*)(?=\\>)";
            var regex = new Regex(pattern);
            var match = regex.Match(calledMethod);

            return match.Groups[1].Value;
        }

        public async Task ProcessAutoTest(TestResult testResult)
        {
            var traceJson = GetTraceJson(testResult);
            var parameters = _parser.GetParameters(traceJson);
            var autoTest = _parser.GetAutoTest(testResult, parameters);
            autoTest.Message = _parser.GetMessage(traceJson);

            var attachmentIds = new List<Guid>();
            var (autoTestSteps, testCaseSteps) =
                await GetStepsWithAttachments(traceJson, autoTest.MethodName, autoTest.Classname, attachmentIds);

            autoTest.Setup = autoTestSteps
                .Where(x => x.CallerMethodType == CallerMethodType.Setup)
                .Select(AutoTestStep.ConvertFromStep)
                .ToList();

            autoTest.Steps = autoTestSteps
                .Where(x => x.CallerMethodType == CallerMethodType.TestMethod)
                .Select(AutoTestStep.ConvertFromStep)
                .ToList();

            autoTest.Teardown = autoTestSteps
                .Where(x => x.CallerMethodType == CallerMethodType.Teardown)
                .Select(AutoTestStep.ConvertFromStep)
                .ToList();


            var existAutotest = await _apiClient.GetAutotestByExternalId(autoTest.ExternalId);

            if (existAutotest == null)
            {
                existAutotest = await _apiClient.CreateAutotest(autoTest);
            }
            else
            {
                autoTest.IsFlaky = existAutotest.IsFlaky;

                await _apiClient.UpdateAutotest(autoTest);
            }

            if (autoTest.WorkItemIds.Count > 0)
            {
                await LinkAutotestToWorkItem(existAutotest.Id.ToString(), autoTest.WorkItemIds);
            }

            if (!string.IsNullOrEmpty(testResult.ErrorMessage))
            {
                autoTest.Message += Environment.NewLine + testResult.ErrorMessage;
            }

            var autoTestResultRequestBody = GetAutoTestResultsForTestRunModel(testResult, testCaseSteps, autoTest,
                traceJson, parameters, attachmentIds);

            await _apiClient.SubmitResultToTestRun(_tmsSettings.TestRunId, autoTestResultRequestBody);
        }

        private AutoTestResult GetAutoTestResultsForTestRunModel(TestResult testResult,
            IReadOnlyCollection<Step> testCaseSteps,
            AutoTest autoTest, string traceJson, Dictionary<string, string>? parameters,
            List<Guid> attachmentIds)
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
                Links = _parser.GetLinks(traceJson),
                Message = autoTest.Message!.TrimStart(Environment.NewLine.ToCharArray()),
                Parameters = parameters!,
                Attachments = attachmentIds,
            };
            if (!string.IsNullOrEmpty(testResult.ErrorStackTrace))
            {
                autoTestResultRequestBody.Traces = testResult.ErrorStackTrace.TrimStart();
            }

            return autoTestResultRequestBody;
        }

        private async Task LinkAutotestToWorkItem(string autotestId, IEnumerable<string> workItemIds)
        {
            foreach (var workItemId in workItemIds)
            {
                await _apiClient.LinkAutoTestToWorkItem(autotestId, workItemId);
            }
        }

        private static string GetTraceJson(TestResult testResult)
        {
            var debugTraceMessages = testResult.Messages.Select(x => x.Text);
            var traceJson = string.Join("\n", debugTraceMessages);

            return traceJson;
        }

        private static Step? MapStep(Step? step, StepResult stepResult)
        {
            if (step == null)
            {
                return null;
            }

            step.CompletedOn = stepResult.CompletedOn;
            step.Duration = stepResult.Duration;
            step.Result = stepResult.Result;
            step.Outcome = stepResult.Outcome;

            return step;
        }
    }
}