using Microsoft.Extensions.Logging;
using TestIT.ApiClient.Api;
using TestIT.ApiClient.Client;
using TestIT.ApiClient.Model;
using Tms.Adapter.Core.Configurator;
using Tms.Adapter.Core.Models;
using Link = Tms.Adapter.Core.Models.Link;
using LinkType = TestIT.ApiClient.Model.LinkType;

namespace Tms.Adapter.Core.Client;

public class TmsClient : ITmsClient
{
    private readonly ILogger<TmsClient> _logger;
    private readonly TmsSettings _settings;
    private readonly TestRunsApi _testRuns;
    private readonly AttachmentsApi _attachments;
    private readonly AutoTestsApi _autoTests;
    private readonly int MAX_TRIES = 10;
    private readonly int WAITING_TIME = 200;

    public TmsClient(ILogger<TmsClient> logger, TmsSettings settings)
    {
        _logger = logger;
        _settings = settings;

        var cfg = new Configuration { BasePath = settings.Url };
        cfg.AddApiKeyPrefix("Authorization", "PrivateToken");
        cfg.AddApiKey("Authorization", settings.PrivateToken);

        var httpClientHandler = new HttpClientHandler();

        if (!_settings.CertValidation)
        {
            httpClientHandler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
        }

        _testRuns = new TestRunsApi(new HttpClient(httpClientHandler), cfg);
        _attachments = new AttachmentsApi(new HttpClient(httpClientHandler), cfg);
        _autoTests = new AutoTestsApi(new HttpClient(httpClientHandler), cfg);
    }

    public async Task<bool> IsAutotestExist(string externalId)
    {
        var autotest = await GetAutotestByExternalId(externalId);

        return autotest != null;
    }

    public async Task CreateAutotest(TestContainer result, ClassContainer container)
    {
        _logger.LogDebug("Creating autotest {ExternalId}", result.ExternalId);

        var model = Converter.ConvertAutoTestDtoToPostModel(result, container, _settings.ProjectId);
        model.ShouldCreateWorkItem = _settings.AutomaticCreationTestCases;

        await _autoTests.CreateAutoTestAsync(model);

        _logger.LogDebug("Create autotest {ExternalId} is successfully", result.ExternalId);
    }

    public async Task UpdateAutotest(TestContainer result, ClassContainer container)
    {
        _logger.LogDebug("Updating autotest {ExternalId}", result.ExternalId);

        var autotest = await GetAutotestByExternalId(result.ExternalId);
        var model = Converter.ConvertAutoTestDtoToPutModel(result, container, _settings.ProjectId);
        model.IsFlaky = autotest?.IsFlaky;

        await _autoTests.UpdateAutoTestAsync(model);

        _logger.LogDebug("Update autotest {ExternalId} is successfully", result.ExternalId);
    }

    public async Task UpdateAutotest(string externalId, List<Link> links, string externalKey)
    {
        _logger.LogDebug("Updating links property for autotest {ExternalId}: {@Links}", externalId, links);

        var autotest = await GetAutotestByExternalId(externalId);

        if (autotest == null)
        {
            _logger.LogError("Autotest with {ID} not found", externalId);
            return;
        }
        
        var putLinks = links.Select(l => new LinkPutModel(url: l.Url)
            {
                Title = l.Title,
                Description = l.Description,
                Type = l.Type != null
                    ? (LinkType?)Enum.Parse(typeof(LinkType), l.Type.ToString())
                    : null
            }
        ).ToList();

        var operations = new List<Operation>
        {
            new()
            {
                Path = nameof(AutoTestPutModel.Links),
                Value = putLinks,
                Op = "Add"
            },
            new()
            {
                Path = nameof(AutoTestPutModel.ExternalKey),
                Value = externalKey,
                Op = "Replace"
            }
        };

        await _autoTests.ApiV2AutoTestsIdPatchAsync(autotest.Id, operations);

        _logger.LogDebug("Update autotest {ExternalId} is successfully", externalId);
    }

    public async Task LinkAutoTestToWorkItems(string autotestId, IEnumerable<string> workItemIds)
    {
        foreach (var workItemId in workItemIds)
        {
            _logger.LogDebug(
                "Linking autotest {AutotestId} to workitem {WorkitemId}",
                autotestId,
                workItemId);

            for (var attempts = 0; attempts < MAX_TRIES; attempts++)
            {
                try
                {
                    await _autoTests.LinkAutoTestToWorkItemAsync(autotestId, new WorkItemIdModel(workItemId ?? string.Empty)).ConfigureAwait(false);
                    _logger.LogDebug(
                        "Link autotest {AutotestId} to workitem {WorkitemId} is successfully",
                        autotestId,
                        workItemId);

                    return;
                }
                catch (ApiException e)
                {
                    _logger.LogError(
                         "Cannot link autotest {AutotestId} to work item {WorkItemId}: work item does not exist",
                         autotestId,
                         workItemId);

                    Thread.Sleep(WAITING_TIME);
                }
            }
        }

    }

    public async Task DeleteAutoTestLinkFromWorkItem(string autotestId, string workItemId)
    {
        _logger.LogDebug(
            "Unlink autotest {AutotestId} from workitem {WorkitemId}",
            autotestId,
            workItemId);

        for (var attempts = 0; attempts < MAX_TRIES; attempts++)
        {
            try
            {
                await _autoTests.DeleteAutoTestLinkFromWorkItemAsync(autotestId, workItemId);
                _logger.LogDebug(
                    "Unlink autotest {AutotestId} from workitem {WorkitemId} is successfully",
                    autotestId,
                    workItemId);

                return;
            }
            catch (ApiException e)
            {
                _logger.LogError(
                    "Cannot link autotest {AutotestId} to work item {WorkitemId}",
                    autotestId,
                    workItemId);

                Thread.Sleep(WAITING_TIME);
            }
        }
    }

    public async Task<List<WorkItemIdentifierModel>> GetWorkItemsLinkedToAutoTest(string autotestId)
    {
        return await _autoTests.GetWorkItemsLinkedToAutoTestAsync(autotestId);
    }

    public async Task SubmitTestCaseResult(TestContainer result, ClassContainer container)
    {
        _logger.LogDebug("Submitting test result {@Result} to test run {Id}", result, _settings.TestRunId);

        var model = Converter.ConvertResultToModel(result, container, _settings.ConfigurationId);
        await _testRuns.SetAutoTestResultsForTestRunAsync(new Guid(_settings.TestRunId),
            new List<AutoTestResultsForTestRunModel> { model });

        _logger.LogDebug("Submit test result to test run {Id} is successfully", _settings.TestRunId);
    }

    public async Task<string> UploadAttachment(string fileName, Stream content)
    {
        _logger.LogDebug("Uploading attachment {Name}", fileName);

        var response = await _attachments.ApiV2AttachmentsPostAsync(
            new FileParameter(
                filename: Path.GetFileName(fileName),
                content: content,
                contentType: MimeTypes.GetMimeType(fileName))
        );

        _logger.LogDebug("Upload attachment {@Attachment} is successfully", response);

        return response.Id.ToString();
    }

    public async Task CreateTestRun()
    {
        _logger.LogDebug("Creating test run");

        if (!string.IsNullOrEmpty(_settings.TestRunId))
        {
            _logger.LogDebug("Test run id : {ID}", _settings.TestRunId);

            return;
        }

        var testRunV2PostShortModel = new TestRunV2PostShortModel
        {
            ProjectId = new Guid(_settings.ProjectId),
            Name = (string.IsNullOrEmpty(_settings.TestRunName) ? null : _settings.TestRunName)!
        };
        var testRun = await _testRuns.CreateEmptyAsync(testRunV2PostShortModel);

        _settings.TestRunId = testRun.Id.ToString();

        _logger.LogDebug("Test run id : {ID}", _settings.TestRunId);
    }

    public async Task CompleteTestRun()
    {
        _logger.LogDebug("Completing test run");

        if (!string.IsNullOrEmpty(_settings.TestRunId))
        {
            return;
        }

        var testRun = await _testRuns.GetTestRunByIdAsync(new Guid(_settings.TestRunId));

        if (testRun.StateName != TestRunState.Completed)
        {
            await _testRuns.CompleteTestRunAsync(new Guid(_settings.TestRunId));
        }

        _logger.LogDebug("Complete test run is successfully");
    }

    public async Task<AutoTestModel?> GetAutotestByExternalId(string externalId)
    {
        _logger.LogDebug("Getting autotest by external id {Id}", externalId);

        var filter = new ApiV2AutoTestsSearchPostRequest(
            filter: new AutotestsSelectModelFilter
            {
                ExternalIds = new List<string> { externalId },
                ProjectIds = new List<Guid> { new Guid(_settings.ProjectId) },
                IsDeleted = false
            },
            includes: new AutotestsSelectModelIncludes()
        );

        var autotests = await _autoTests.ApiV2AutoTestsSearchPostAsync(apiV2AutoTestsSearchPostRequest: filter);
        var autotest = autotests.FirstOrDefault();

        _logger.LogDebug(
            "Get autotest {@Autotest} by external id {Id}",
            autotest,
            externalId);

        return autotest;
    }
}