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

    public async Task<TestRunV2ApiResult?> CreateTestRunAsync()
    {
        var testRunV2PostShortModel = new CreateEmptyTestRunApiModel
        {
            ProjectId = new Guid(settings.ProjectId ?? string.Empty),
            Name = (string.IsNullOrEmpty(settings.TestRunName) ? null : settings.TestRunName)!
        };

        logger.LogDebug("Creating test run {@TestRun}", testRunV2PostShortModel);

        var testRun = await testRunsApi.CreateEmptyAsync(testRunV2PostShortModel).ConfigureAwait(false) ?? throw new Exception($"Could not find project with id: {settings.ProjectId}");
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

    public async Task SubmitResultToTestRunAsync(string? id, AutoTestResult result)
    {
        logger.LogDebug("Submitting test result {@Result} to test run {@Id}", result, id);

        var testRunId = new Guid(id ?? string.Empty);
        var model = Converter.ConvertResultToModel(result, settings.ConfigurationId);

        if (settings is not { AdapterMode: 0, IgnoreParameters: true })
        {
            await testRunsApi.SetAutoTestResultsForTestRunAsync(testRunId, [model])
                .ConfigureAwait(false);
            logger.LogDebug("Submit test result to test run {Id} completed successfully", id);
            
            return;
        }

        var currentTestRun = testRunContext.GetCurrentTestRun();
        var matchingResults = currentTestRun?.TestResults?
            .Where(x => x.AutoTest.ExternalId == result.ExternalId)
            .ToList();

        if (matchingResults is { Count: 0 })
        {
            throw new InvalidOperationException($"No matching autotest found for ExternalId: {result.ExternalId}");
        }

        foreach (var matchingResult in matchingResults!)
        {
            model.Parameters = matchingResult.Parameters;
            await testRunsApi.SetAutoTestResultsForTestRunAsync(testRunId, [model])
                .ConfigureAwait(false);
            logger.LogDebug("Submitted result for test point with parameters: {@Parameters}",
                matchingResult.Parameters);
        }
    }

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

    public async Task<AutoTestModel> CreateAutotestAsync(AutoTest dto)
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

    public async Task LinkAutoTestToWorkItemAsync(string autotestId, IEnumerable<string?> workItemIds)
    {
        foreach (var workItemId in workItemIds)
        {
            logger.LogDebug(
                "Linking autotest {AutotestId} to workitem {WorkitemId}",
                autotestId,
                workItemId);

            for (var attempts = 0; attempts < MAX_TRIES; attempts++)
            {
                try
                {
                    await autoTestsApi.LinkAutoTestToWorkItemAsync(autotestId, new WorkItemIdApiModel(workItemId ?? string.Empty)).ConfigureAwait(false);
                    logger.LogDebug(
                        "Link autotest {AutotestId} to workitem {WorkitemId} is successfully",
                    autotestId,
                    workItemId);

                    return;
                }
                catch (ApiException)
                {
                    logger.LogError(
                         "Cannot link autotest {AutotestId} to work item {WorkItemId}",
                    autotestId,
                    workItemId);

                    Thread.Sleep(WAITING_TIME);
                }
            }
        }

    }

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
            catch (ApiException)
            {
                logger.LogError(
                    "Cannot link autotest {AutotestId} to work item {WorkitemId}",
                    autotestId,
                    workItemId);

                Thread.Sleep(WAITING_TIME);
            }
        }
    }

    public async Task<List<WorkItemIdentifierModel>> GetWorkItemsLinkedToAutoTestAsync(string autotestId)
    {
        return await autoTestsApi.GetWorkItemsLinkedToAutoTestAsync(autotestId);
    }

    public async Task<ProjectModel> GetProjectByIdAsync()
    {
        return await projectsApi.GetProjectByIdAsync(settings.ProjectId!).ConfigureAwait(false);
    }
}
