using Microsoft.Extensions.Logging;
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
                              TmsSettings settings,
                              ITestRunContextService testRunContext)
{
    private readonly int MAX_TRIES = 10;
    private readonly int WAITING_TIME = 200;

    public async Task<TestRunV2GetModel?> CreateTestRunAsync()
    {
        var testRunV2PostShortModel = new TestRunV2PostShortModel
        {
            ProjectId = new Guid(settings.ProjectId ?? string.Empty),
            Name = (string.IsNullOrEmpty(settings.TestRunName) ? null : settings.TestRunName)!
        };

        logger.LogDebug("Creating test run {@TestRun}", testRunV2PostShortModel);

        var testRun = await testRunsApi.CreateEmptyAsync(testRunV2PostShortModel).ConfigureAwait(false) ?? throw new Exception($"Could not find project with id: {settings.ProjectId}");
        logger.LogDebug("Created test run {@TestRun}", testRun);

        return testRun;
    }

    public async Task<TestRunV2GetModel?> GetTestRunAsync(string testRunId)
    {
        logger.LogDebug("Getting test run {@TestRunId}", testRunId);
        
        return await testRunsApi.GetTestRunByIdAsync(new Guid(testRunId ?? string.Empty)).ConfigureAwait(false);
    }
    
    public List<string?> GetAutoTestsForRunAsync(TestRunV2GetModel testRun)
    {
        logger.LogDebug("Getting autotests for run from test run {Id}", testRun.Id);

        var autotests = testRun.TestResults.Where(x => !x.AutoTest.IsDeleted).Select(x => x.AutoTest.ExternalId).ToList();

        logger.LogDebug(
            "Autotests for run from test run {Id}: {@Autotests}",
            testRun.Id,
            autotests);

        return autotests as List<string?>;
    }

    public async Task SubmitResultToTestRunAsync(string? id, AutoTestResult result)
    {
        logger.LogDebug("Submitting test result {@Result} to test run {@Id}", result, id);

        var model = Converter.ConvertResultToModel(result, settings.ConfigurationId);

        if (settings.AdapterMode == 0 && settings.IgnoreParameters)
        {
            var currentTestRun = testRunContext.GetCurrentTestRun();
            var matchingResults = currentTestRun?.TestResults?
                .Where(x => x.AutoTest.ExternalId == result.ExternalId)
                .ToList();

            if (matchingResults?.Any() == true)
            {
                foreach (var matchingResult in matchingResults)
                {
                    model.Parameters = matchingResult.Parameters;
                    await testRunsApi.SetAutoTestResultsForTestRunAsync(new Guid(id ?? string.Empty), [model])
                        .ConfigureAwait(false);
                    logger.LogDebug("Submitted result for test point with parameters: {@Parameters}",
                        matchingResult.Parameters);
                }

                return;
            }
        }
        
        await testRunsApi.SetAutoTestResultsForTestRunAsync(new Guid(id ?? string.Empty), [model])
            .ConfigureAwait(false);
        logger.LogDebug("Submit test result to test run {Id} completed successfully", id);
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

    public async Task<AutoTestModel?> GetAutotestByExternalIdAsync(string? externalId)
    {
        logger.LogDebug("Getting autotest by external id {Id}", externalId);

        var filter = new ApiV2AutoTestsSearchPostRequest(
            filter: new AutotestsSelectModelFilter
            {
                ExternalIds = [externalId ?? string.Empty],
                ProjectIds = settings.ProjectId == null ? [] : [new Guid(settings.ProjectId)],
                IsDeleted = false
            },
            includes: new AutotestsSelectModelIncludes()
        );

        var autotests = await autoTestsApi.ApiV2AutoTestsSearchPostAsync(apiV2AutoTestsSearchPostRequest: filter).ConfigureAwait(false);
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
                    await autoTestsApi.LinkAutoTestToWorkItemAsync(autotestId, new WorkItemIdModel(workItemId ?? string.Empty)).ConfigureAwait(false);
                    logger.LogDebug(
                        "Link autotest {AutotestId} to workitem {WorkitemId} is successfully",
                    autotestId,
                    workItemId);

                    return;
                }
                catch (ApiException e)
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
            catch (ApiException e)
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
}
