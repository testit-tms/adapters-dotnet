using Microsoft.Extensions.Logging;
using TestIt.Client.Api;
using TestIt.Client.Client;
using TestIt.Client.Model;
using Tms.Adapter.Core.Models;
using TmsRunner.Models;
using LinkType = TestIt.Client.Model.LinkType;

namespace Tms.Adapter.Core.Client;

public class TmsClient : ITmsClient
{
    private readonly ILogger<TmsClient> _logger;
    private readonly TmsSettings _settings;
    private readonly TestRunsApi _testRuns;
    private readonly AttachmentsApi _attachments;
    private readonly AutoTestsApi _autoTests;

    public TmsClient(ILogger<TmsClient> logger, TmsSettings settings)
    {
        _logger = logger;
        _settings = settings;

        var cfg = new Configuration { BasePath = settings.Url };
        cfg.AddApiKeyPrefix("Authorization", "PrivateToken");
        cfg.AddApiKey("Authorization", settings.PrivateToken);

        var httpClientHandler = new HttpClientHandler();
        httpClientHandler.ServerCertificateCustomValidationCallback = (_, _, _, _) => _settings.CertValidation;

        _testRuns = new TestRunsApi(new HttpClient(), cfg, httpClientHandler);
        _attachments = new AttachmentsApi(new HttpClient(), cfg, httpClientHandler);
        _autoTests = new AutoTestsApi(new HttpClient(), cfg, httpClientHandler);
    }

    public async Task CreatTestRun()
    {
        var createTestRunRequestBody = new TestRunV2PostShortModel
        {
            ProjectId = new Guid(_settings.ProjectId),
            Name = (string.IsNullOrEmpty(_settings.TestRunName) ? null : _settings.TestRunName)!
        };

        _logger.LogDebug("Creating test run {@TestRun}", createTestRunRequestBody);

        var testRun = await _testRuns.CreateEmptyAsync(createTestRunRequestBody);

        if (testRun is null)
        {
            throw new Exception($"Could not find project with id: {_settings.ProjectId}");
        }

        _logger.LogDebug("Create test run {@TestRun} is successfully", testRun);

        await _testRuns.StartTestRunAsync(testRun.Id);

        _settings.TestRunId = testRun.Id.ToString();
    }

    public async Task<bool> IsAutotestExist(string externalId)
    {
        var autotest = await GetAutotestByExternalId(externalId);

        return autotest != null;
    }

    public async Task CreateAutotest(TestResult result, TestResultContainer container)
    {
        _logger.LogDebug("Creating autotest {ExternalId}", result.ExternalId);

        var model = Converter.ConvertAutoTestDtoToPostModel(result, container, _settings.ProjectId);

        await _autoTests.CreateAutoTestAsync(model);

        _logger.LogDebug("Create autotest {ExternalId} is successfully", result.ExternalId);
    }

    public async Task UpdateAutotest(TestResult result, TestResultContainer container)
    {
        _logger.LogDebug("Updating autotest {ExternalId}", result.ExternalId);

        var autotest = await GetAutotestByExternalId(result.ExternalId);
        var model = Converter.ConvertAutoTestDtoToPutModel(result, container, _settings.ProjectId);
        model.IsFlaky = autotest.IsFlaky;

        await _autoTests.UpdateAutoTestAsync(model);

        _logger.LogDebug("Update autotest {ExternalId} is successfully", result.ExternalId);
    }

    public async Task UpdateAutotest(string externalId, List<Link> links)
    {
        _logger.LogDebug("Updating links property for autotest {ExternalId}: {@Links}", externalId, links);
        
        var autotest = await GetAutotestByExternalId(externalId);
        var putLinks = links.Select(l => new LinkPutModel(
            title: l.Title,
            url: l.Url,
            description: l.Description,
            type: (LinkType?)Enum.Parse(typeof(LinkType), l.Type.ToString()))
        ).ToList();

        var operations = new List<Operation>
        {
            new()
            {
                Path = nameof(AutoTestPutModel.Links),
                Value = putLinks,
                Op = "Add"
            }
        };

        await _autoTests.ApiV2AutoTestsIdPatchAsync(autotest.Id, operations);

        _logger.LogDebug("Update autotest {ExternalId} is successfully", externalId);
    }

    public async Task LinkAutoTestToWorkItems(string externalId, IEnumerable<string> workItemIds)
    {
        var autotest = await GetAutotestByExternalId(externalId);

        foreach (var workItemId in workItemIds)
        {
            _logger.LogDebug(
                "Linking autotest {AutotestId} to workitem {WorkitemId}",
                autotest.Id,
                workItemId);

            try
            {
                await _autoTests.LinkAutoTestToWorkItemAsync(autotest.Id.ToString(), new WorkItemIdModel(workItemId));
            }
            catch (ApiException e) when (e.Message.Contains("does not exist"))
            {
                _logger.LogError(
                    "Cannot link autotest {AutotestId} to workitem {WorkitemId}: workitem was not found",
                    autotest.Id,
                    workItemId);
                return;
            }

            _logger.LogDebug(
                "Link autotest {AutotestId} to workitem {WorkitemId} is successfully",
                autotest.Id,
                workItemId);
        }
    }

    public async Task SubmitTestCaseResult(TestResult result, TestResultContainer container)
    {
        _logger.LogDebug("Submitting test result {@Result} to test run {Id}", result, _settings.TestRunId);

        var model = Converter.ConvertResultToModel(result, container, _settings.ConfigurationId);
        await _testRuns.SetAutoTestResultsForTestRunAsync(new Guid(_settings.TestRunId),
            new List<AutoTestResultsForTestRunModel> { model });

        _logger.LogDebug("Submit test result to test run {Id} is successfully", _settings.TestRunId);
    }

    private async Task<AutoTestModel?> GetAutotestByExternalId(string externalId)
    {
        _logger.LogDebug("Getting autotest by external id {Id}", externalId);

        var filter = new AutotestsSelectModel
        {
            Filter = new AutotestFilterModel
            {
                ExternalIds = new List<string> { externalId },
                ProjectIds = new List<Guid> { new Guid(_settings.ProjectId) }
            }
        };

        var autotests = await _autoTests.ApiV2AutoTestsSearchPostAsync(autotestsSelectModel: filter);
        var autotest = autotests.FirstOrDefault();

        _logger.LogDebug(
            "Get autotest {@Autotest} by external id {Id}",
            autotest,
            externalId);

        return autotest;
    }
}