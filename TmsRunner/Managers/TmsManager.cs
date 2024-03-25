using Microsoft.Extensions.Logging;
using TestIT.ApiClient.Api;
using TestIT.ApiClient.Client;
using TestIT.ApiClient.Model;
using TmsRunner.Models;
using TmsRunner.Models.AutoTest;
using TmsRunner.Utils;

namespace TmsRunner.Managers;

public sealed class TmsManager(ILogger<TmsManager> logger,
                              IAttachmentsApiAsync attachmentsApi,
                              IAutoTestsApiAsync autoTestsApi,
                              ITestRunsApiAsync testRunsApi,
                              TmsSettings settings)
{
    public async Task<string?> CreateTestRunAsync()
    {
        var createTestRunRequestBody = new CreateEmptyRequest
        {
            ProjectId = new Guid(settings.ProjectId ?? string.Empty),
            Name = (string.IsNullOrEmpty(settings.TestRunName) ? null : settings.TestRunName)!
        };

        logger.LogDebug("Creating test run {@TestRun}", createTestRunRequestBody);

        var testRun = await testRunsApi.CreateEmptyAsync(createTestRunRequestBody).ConfigureAwait(false) ?? throw new Exception($"Could not find project with id: {settings.ProjectId}");
        logger.LogDebug("Created test run {@TestRun}", testRun);

        return testRun.Id.ToString();
    }

    public async Task<List<string?>> GetAutoTestsForRunAsync(string? testRunId)
    {
        logger.LogDebug("Getting autotests for run from test run {Id}", testRunId);

        var testRun = await testRunsApi.GetTestRunByIdAsync(new Guid(testRunId ?? string.Empty)).ConfigureAwait(false);

        var autotests = testRun.TestResults.Where(x => !x.AutoTest.IsDeleted).Select(x => x.AutoTest.ExternalId).ToList();

        logger.LogDebug(
            "Autotests for run from test run {Id}: {@Autotests}",
            testRunId,
            autotests);

        return autotests as List<string?>;
    }

    public async Task SubmitResultToTestRunAsync(string? id, AutoTestResult result)
    {
        logger.LogDebug("Submitting test result {@Result} to test run {@Id}", result, id);

        var model = Converter.ConvertResultToModel(result, settings.ConfigurationId);
        _ = await testRunsApi.SetAutoTestResultsForTestRunAsync(new Guid(id ?? string.Empty), [model]).ConfigureAwait(false);

        logger.LogDebug("Submit test result to test run {Id} is successfully", id);
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

    public async Task<bool> TryLinkAutoTestToWorkItemAsync(string autotestId, IEnumerable<string?> workItemIds)
    {
        foreach (var workItemId in workItemIds)
        {
            logger.LogDebug(
                "Linking autotest {AutotestId} to workitem {WorkitemId}",
                autotestId,
                workItemId);

            try
            {
                await autoTestsApi.LinkAutoTestToWorkItemAsync(autotestId, new LinkAutoTestToWorkItemRequest(workItemId ?? string.Empty)).ConfigureAwait(false);
            }
            catch (ApiException e) when (e.Message.Contains("does not exist"))
            {
                logger.LogError(
                     "Cannot link autotest {AutotestId} to work item {WorkItemId}: work item does not exist",
                     autotestId,
                     workItemId);

                return false;
            }

            logger.LogDebug(
                "Link autotest {AutotestId} to workitem {WorkitemId} is successfully",
                autotestId,
                workItemId);
        }

        return true;
    }
}