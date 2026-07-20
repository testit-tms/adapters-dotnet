using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using TestIT.ApiClient.Api;
using TestIT.ApiClient.Client;
using TestIT.ApiClient.Model;
using TmsRunner.Entities;
using TmsRunner.Entities.AutoTest;
using TmsRunner.Services;
using TmsRunner.Utils;
using CoreConverter = Tms.Adapter.Core.Client.Converter;
using AutoTest = TmsRunner.Entities.AutoTest.AutoTest;

namespace TmsRunner.Managers;

public sealed class TmsManager(ILogger<TmsManager> logger,
                              IAttachmentsApiAsync attachmentsApi,
                              IAutoTestsApiAsync autoTestsApi,
                              ITestRunsApiAsync testRunsApi,
                              ITestResultsApiAsync testResultsApi,
                              IProjectsApiAsync projectsApi,
                              TmsSettings settings,
                              ITestRunContextService testRunContext)
{
    private readonly int MAX_TRIES = 10;
    private readonly int WAITING_TIME = 200;
    private readonly int TESTS_LIMIT = 100;
    private readonly HashSet<string> _absentWorkItemIds = new(StringComparer.Ordinal);

    public async Task<TestRunV2ApiResult?> CreateTestRunAsync()
    {
        var testRunV2PostShortModel = new CreateEmptyTestRunApiModel
        {
            ProjectId = new Guid(settings.ProjectId ?? string.Empty),
            Name = (string.IsNullOrEmpty(settings.TestRunName) ? null : settings.TestRunName)!
        };

        logger.LogDebug("Creating test run {@TestRun}", testRunV2PostShortModel);

        var testRun = await testRunsApi.CreateEmptyAsync(testRunV2PostShortModel).ConfigureAwait(false) 
                      ?? throw new ArgumentException($"Could not find project with id: {settings.ProjectId}");
        logger.LogDebug("Created test run {@TestRun}", testRun);

        return testRun;
    }

    public async Task<TestRunV2ApiResult?> GetTestRunAsync()
    {
        logger.LogDebug("Getting test run {@TestRunId}", settings.TestRunId);
        
        return await testRunsApi.GetTestRunByIdAsync(new Guid(settings.TestRunId ?? string.Empty)).ConfigureAwait(false);
    }

    public async Task UpdateTestRunAsync(TestRunV2ApiResult testRun)
    {
        logger.LogDebug("Updating test run {@TestRunId}", settings.TestRunId);

        var model = Converter.BuildUpdateEmptyTestRunApiModel(testRun);

        await testRunsApi.UpdateEmptyAsync(model).ConfigureAwait(false);
    }

    public async Task<List<string>> GetExternalIdsForRunAsync()
    {
        logger.LogDebug("Getting test results for run from test run {TestRunId} with configuration {ConfigurationId}", settings.TestRunId, settings.ConfigurationId);

        var externalIds = new List<string>();
        var skip = 0;
        var model = Converter.BuildTestResultsFilterApiModel(settings.TestRunId!, settings.ConfigurationId!);

        while (true)
        {
            var testResults = await GetTestResults(skip, model);

            if (testResults.Count != 0)
            {
                externalIds.AddRange(testResults.Select(x => x.AutotestExternalId).ToList());
                skip += TESTS_LIMIT;

                continue;
            }

            return externalIds;
        }
    }

    private async Task<List<TestResultShortResponse>> GetTestResults(int skip, TestResultsFilterApiModel model)
    {
        return await testResultsApi.ApiV2TestResultsSearchPostAsync(skip, TESTS_LIMIT, null!, null!, null!, model);
    }

    public async Task SubmitResultToTestRunAsync(string? id, AutoTestResult result, bool forceInProgressStatus = false)
    {
        logger.LogDebug("Submitting test result {@Result} to test run {@Id}", result, id);

        var testRunId = new Guid(id ?? string.Empty);
        var model = Converter.ConvertResultToModel(result, settings.ConfigurationId, forceInProgressStatus);
        Utils.HtmlEscapeUtils.EscapeHtmlInObject(model);

        if (forceInProgressStatus)
        {
            await testRunsApi.SetAutoTestResultsForTestRunAsync(testRunId, [model]).ConfigureAwait(false);
            logger.LogDebug("Submitted InProgress test result to test run {Id}", id);
            return;
        }

        if (settings is { AdapterMode: 0, IgnoreParameters: true })
        {
            await SubmitWithParameterMatchingAsync(model, result, testRunId).ConfigureAwait(false);
            return;
        }

        await SubmitWithUpdateOrPostAsync(model, result.ExternalId, testRunId).ConfigureAwait(false);
    }

    private async Task SubmitWithUpdateOrPostAsync(
        AutoTestResultsForTestRunModel model,
        string? externalId,
        Guid testRunId)
    {
        var existing = await FindTestResultByExternalIdAsync(externalId).ConfigureAwait(false);

        if (existing != null && !ShouldSendFinalResult(model, existing))
        {
            model.Duration = GetAccumulatedDuration(existing.Duration, model.Duration);

            var update = CoreConverter.ConvertResultToUpdateModel(model);
            Utils.HtmlEscapeUtils.EscapeHtmlInObject(update);
            await testResultsApi.ApiV2TestResultsIdPutAsync(existing.Id, update).ConfigureAwait(false);
            logger.LogDebug("Updated test result {TestResultId} for {ExternalId}", existing.Id, externalId);
            return;
        }

        await testRunsApi.SetAutoTestResultsForTestRunAsync(testRunId, [model]).ConfigureAwait(false);
        logger.LogDebug("Submitted test result to test run {TestRunId} for {ExternalId}", testRunId, externalId);
    }

    private static long? GetAccumulatedDuration(long? submittedDuration, long? currentDuration)
    {
        if (submittedDuration is null || currentDuration is null)
        {
            return currentDuration;
        }

        return checked(submittedDuration.Value + Math.Max(currentDuration.Value, 1));
    }

    [PerformanceSensitive]
    private async Task SubmitWithParameterMatchingAsync(
        AutoTestResultsForTestRunModel model,
        AutoTestResult result,
        Guid testRunId)
    {
        var matchingResults = testRunContext.GetCurrentTestRun()?.TestResults?
            .Where(x => x.AutoTest.ExternalId == result.ExternalId)
            .ToList();

        if (matchingResults is { Count: 0 })
        {
            throw new InvalidOperationException($"No matching autotest found for ExternalId: {result.ExternalId}");
        }

        foreach (var matchingResult in matchingResults!)
        {
            model.Parameters = matchingResult.Parameters;

            if (IsInProgress(matchingResult) && model.StatusType != TestStatusType.InProgress)
            {
                await testRunsApi.SetAutoTestResultsForTestRunAsync(testRunId, [model]).ConfigureAwait(false);
                logger.LogDebug("Submitted final result for test point with parameters: {@Parameters}", matchingResult.Parameters);
                continue;
            }

            var update = CoreConverter.ConvertResultToUpdateModel(model);
            Utils.HtmlEscapeUtils.EscapeHtmlInObject(update);
            await testResultsApi.ApiV2TestResultsIdPutAsync(matchingResult.Id, update).ConfigureAwait(false);
            logger.LogDebug("Updated test result {TestResultId} with parameters: {@Parameters}", matchingResult.Id, matchingResult.Parameters);
        }
    }

    private async Task<TestResultShortResponse?> FindTestResultByExternalIdAsync(string? externalId)
    {
        if (string.IsNullOrWhiteSpace(externalId) || string.IsNullOrWhiteSpace(settings.TestRunId))
        {
            return null;
        }

        var filter = new TestResultsFilterApiModel
        {
            TestRunIds = [new Guid(settings.TestRunId)],
            ConfigurationIds = settings.ConfigurationId == null ? null : [new Guid(settings.ConfigurationId)],
        };

        var results = await testResultsApi.ApiV2TestResultsSearchPostAsync(0, TESTS_LIMIT, null!, null!, null!, filter)
            .ConfigureAwait(false);

        return results.FirstOrDefault(r => r.AutotestExternalId == externalId);
    }

    private static bool ShouldSendFinalResult(AutoTestResultsForTestRunModel model, TestResultShortResponse existing) =>
        IsInProgress(existing) && model.StatusType != TestStatusType.InProgress;

    private static bool IsInProgress(TestResultShortResponse result) =>
        result.Status?.Type == TestStatusApiType.InProgress;

    private static bool IsInProgress(TestResultV2GetModel result) =>
        string.Equals(result.Outcome, "InProgress", StringComparison.OrdinalIgnoreCase);

    public async Task<AttachmentModel> UploadAttachmentAsync(string fileName, Stream content)
    {
        logger.LogDebug("Uploading attachment {Name}", fileName);

        var response = await attachmentsApi.ApiV2AttachmentsPostAsync(
            new FileParameter(
                filename: Path.GetFileName(fileName),
                content: content,
                contentType: MimeTypes.GetMimeType(fileName))
        ).ConfigureAwait(false);

        logger.LogDebug("Upload attachment {@Attachment} is successfully", response);

        return response;
    }

    public async Task<AutoTestApiResult?> GetAutotestByExternalIdAsync(string? externalId)
    {
        logger.LogDebug("Getting autotest by external id {Id}", externalId);

        var model = new AutoTestSearchApiModel(
            filter: new AutoTestFilterApiModel
            {
                ExternalIds = [externalId ?? string.Empty],
                ProjectIds = settings.ProjectId == null ? [] : [new Guid(settings.ProjectId)],
                IsDeleted = false
            },
            includes: new AutoTestSearchIncludeApiModel()
        );

        var autotests = await autoTestsApi.ApiV2AutoTestsSearchPostAsync(autoTestSearchApiModel: model).ConfigureAwait(false);
        var autotest = autotests.FirstOrDefault();

        logger.LogDebug(
            "Get autotest {@Autotest} by external id {Id}",
            autotest,
            externalId);

        return autotest;
    }

    public async Task<AutoTestApiResult> CreateAutotestAsync(AutoTest dto)
    {
        logger.LogDebug("Creating autotest {@Autotest}", dto);

        var model = Converter.ConvertAutoTestDtoToPostModel(dto, settings.ProjectId);
        model.ShouldCreateWorkItem = settings.AutomaticCreationTestCases;
        var response = await autoTestsApi.CreateAutoTestAsync(model).ConfigureAwait(false);

        logger.LogDebug("Create autotest {@Autotest} is successfully", response);

        return response;
    }

    public async Task UpdateAutotestAsync(AutoTest dto)
    {
        logger.LogDebug("Updating autotest {@Autotest}", dto);

        var model = Converter.ConvertAutoTestDtoToPutModel(dto, settings.ProjectId);
        await autoTestsApi.UpdateAutoTestAsync(model).ConfigureAwait(false);

        logger.LogDebug("Update autotest {@Autotest} is successfully", model);
    }

    public async Task LinkAutoTestToWorkItemAsync(
        string autotestId,
        IEnumerable<string?> workItemIds)
    {
        var exceptions = new List<Exception>();

        foreach (var workItemId in workItemIds)
        {
            if (string.IsNullOrEmpty(workItemId) ||
                _absentWorkItemIds.Contains(workItemId))
            {
                continue;
            }

            var exception = await TryLinkAutoTestToWorkItemAsync(
                    autotestId,
                    workItemId)
                .ConfigureAwait(false);

            if (exception is not null)
            {
                exceptions.Add(exception);
            }
        }

        if (exceptions.Count > 0)
        {
            throw new AggregateException(
                $"Failed to link autotest {autotestId} to one or more work items.",
                exceptions);
        }
    }

    private async Task<ApiException?> TryLinkAutoTestToWorkItemAsync(
        string autotestId,
        string workItemId)
    {
        for (var attempt = 1; attempt <= MAX_TRIES; attempt++)
        {
            try
            {
                await autoTestsApi.LinkAutoTestToWorkItemAsync(
                        autotestId,
                        new WorkItemIdApiModel(workItemId))
                    .ConfigureAwait(false);

                logger.LogDebug(
                    "Linked autotest {AutotestId} to work item {WorkItemId}",
                    autotestId,
                    workItemId);

                return null;
            }
            catch (ApiException e) when (IsMissingWorkItem(e))
            {
                _absentWorkItemIds.Add(workItemId);

                logger.LogWarning(
                    "Skipping link between autotest {AutotestId} and work item {WorkItemId}: work item does not exist",
                    autotestId,
                    workItemId);

                return null;
            }
            catch (ApiException e)
            {
                logger.LogError(
                    e,
                    "Cannot link autotest {AutotestId} to work item {WorkItemId}; attempt {Attempt} of {MaxTries}",
                    autotestId,
                    workItemId,
                    attempt,
                    MAX_TRIES);

                if (attempt == MAX_TRIES)
                {
                    return e;
                }

                await Task.Delay(WAITING_TIME).ConfigureAwait(false);
            }
        }

        return null;
    }

    private static bool IsMissingWorkItem(ApiException ex) =>
        ex.ErrorCode == 404
        || ex.Message.Contains("NotFoundException", StringComparison.OrdinalIgnoreCase);

    public async Task DeleteAutoTestLinkFromWorkItemAsync(string autotestId, string workItemId)
    {
        logger.LogDebug(
            "Unlink autotest {AutotestId} from workitem {WorkitemId}",
            autotestId,
            workItemId);

        for (var attempts = 0; attempts < MAX_TRIES; attempts++)
        {
            try
            {
                await autoTestsApi.DeleteAutoTestLinkFromWorkItemAsync(autotestId, workItemId);
                logger.LogDebug(
                    "Unlink autotest {AutotestId} from workitem {WorkitemId} is successfully",
                    autotestId,
                    workItemId);

                return;
            }
            catch (ApiException e)
            {
                logger.LogError(
                    e, 
                    "Cannot link autotest {AutotestId} to work item {WorkitemId}",
                    autotestId,
                    workItemId);

                Thread.Sleep(WAITING_TIME);
            }
        }
    }

    public async Task<List<AutoTestWorkItemIdentifierApiResult>> GetWorkItemsLinkedToAutoTestAsync(string autotestId)
    {
        return await autoTestsApi.GetWorkItemsLinkedToAutoTestAsync(autotestId);
    }

    public async Task<DetailedProjectApiResult> GetProjectByIdAsync()
    {
        return await projectsApi.GetProjectByIdAsync(settings.ProjectId!).ConfigureAwait(false);
    }
}
