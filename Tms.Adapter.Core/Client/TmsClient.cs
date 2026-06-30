using Microsoft.Extensions.Logging;
using TestIT.AdaptersApi.Api;
using TestIT.AdaptersApi.Client;
using TestIT.AdaptersApi.Model;
using Tms.Adapter.Core.Configurator;
using Tms.Adapter.Core.Models;
using Tms.Adapter.Core.Utils;
using Link = Tms.Adapter.Core.Models.Link;
using LinkType = TestIT.AdaptersApi.Model.LinkType;

namespace Tms.Adapter.Core.Client;

public sealed class TmsClient : ITmsClient, IDisposable
{
    private readonly ILogger<TmsClient> _logger;
    private readonly TmsSettings _settings;
    private readonly TestRunsApi _testRuns;
    private readonly TestResultsApi _testResults;
    private readonly AttachmentsApi _attachments;
    private readonly AutoTestsApi _autoTests;
    private readonly int MAX_TRIES = 10;
    private readonly int WAITING_TIME = 200;
    private readonly HashSet<string> _absentWorkItemIds = new(StringComparer.Ordinal);

    public TmsClient(ILogger<TmsClient> logger, TmsSettings settings)
    {
        _logger = logger;
        _settings = settings;

        var cfg = AdaptersApiConfiguration.Create(settings, out var httpClient);

        _testRuns = new TestRunsApi(httpClient, cfg);
        _testResults = new TestResultsApi(httpClient, cfg);
        _attachments = new AttachmentsApi(httpClient, cfg);
        _autoTests = new AutoTestsApi(httpClient, cfg);
        AdaptersApiConfiguration.ApplyExceptionFactory(_testRuns);
        AdaptersApiConfiguration.ApplyExceptionFactory(_testResults);
        AdaptersApiConfiguration.ApplyExceptionFactory(_attachments);
        AdaptersApiConfiguration.ApplyExceptionFactory(_autoTests);
    }

    public async Task<bool> IsAutotestExist(string externalId)
    {
        var autotest = await GetAutotestByExternalId(externalId).ConfigureAwait(false);

        return autotest != null;
    }

    public async Task CreateAutotest(TestContainer result, ClassContainer container)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Creating autotest {ExternalId}", result.ExternalId);
        }

        var model = Converter.ConvertAutoTestDtoToPostModel(result, container, _settings.ProjectId);
        model.ShouldCreateWorkItem = _settings.AutomaticCreationTestCases;

        HtmlEscapeUtils.EscapeHtmlInObject(model);

        await _autoTests.ApiAdaptersAutoTestsPostAsync(model).ConfigureAwait(false);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Create autotest {ExternalId} is successfully", result.ExternalId);
        }
    }

    public async Task UpdateAutotest(TestContainer result, ClassContainer container)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Updating autotest {ExternalId}", result.ExternalId);
        }

        var autotest = await GetAutotestByExternalId(result.ExternalId).ConfigureAwait(false);
        var model = Converter.ConvertAutoTestDtoToPutModel(result, container, _settings.ProjectId);
        model.IsFlaky = autotest?.IsFlaky;

        HtmlEscapeUtils.EscapeHtmlInObject(model);

        await _autoTests.ApiAdaptersAutoTestsPutAsync(model).ConfigureAwait(false);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Update autotest {ExternalId} is successfully", result.ExternalId);
        }
    }

    public async Task UpdateAutotest(string externalId, List<Link> links, string externalKey)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Updating links property for autotest {ExternalId}: {@Links}", externalId, links);
        }

        var autotest = await GetAutotestByExternalId(externalId).ConfigureAwait(false);

        if (autotest == null)
        {
            _logger.LogError("Autotest with {ID} not found", externalId);
            return;
        }
        
        var putLinks = links.Select(l => new LinkUpdateApiModel(url: l.Url)
            {
                Title = l.Title!,
                Description = l.Description!,
                Type = l.Type != null
                    ? Enum.Parse<LinkType>(l.Type.ToString())
                    : LinkType.Related
            }
        ).ToList();

        HtmlEscapeUtils.EscapeHtmlInObjectList(putLinks);

        var operations = new List<Operation>
        {
            new()
            {
                Path = nameof(AutoTestUpdateApiModel.Links),
                Value = putLinks,
                Op = "Add"
            },
            new()
            {
                Path = nameof(AutoTestUpdateApiModel.ExternalKey),
                Value = HtmlEscapeUtils.EscapeHtmlTags(externalKey)!,
                Op = "Replace"
            }
        };

        await _autoTests.ApiAdaptersAutoTestsIdPatchAsync(autotest.Id, operations).ConfigureAwait(false);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Update autotest {ExternalId} is successfully", externalId);
        }
    }

    public async Task LinkAutoTestToWorkItems(string autotestId, IEnumerable<string> workItemIds)
    {
        foreach (var workItemId in workItemIds)
        {
            if (string.IsNullOrEmpty(workItemId) || _absentWorkItemIds.Contains(workItemId))
            {
                continue;
            }

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(
                    "Linking autotest {AutotestId} to workitem {WorkitemId}",
                    autotestId,
                    workItemId);
            }

            for (var attempts = 0; attempts < MAX_TRIES; attempts++)
            {
                try
                {
                    var workItemModel = new WorkItemIdApiModel(workItemId);
                    HtmlEscapeUtils.EscapeHtmlInObject(workItemModel);

                    await _autoTests.ApiAdaptersAutoTestsIdWorkItemsPostAsync(autotestId, workItemModel).ConfigureAwait(false);
                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug(
                            "Link autotest {AutotestId} to workitem {WorkitemId} is successfully",
                            autotestId,
                            workItemId);
                    }

                    break;
                }
                catch (ApiException e) when (IsMissingWorkItem(e))
                {
                    _absentWorkItemIds.Add(workItemId);
                    _logger.LogWarning(
                        "Skip linking autotest {AutotestId} to work item {WorkItemId}: work item does not exist",
                        autotestId,
                        workItemId);
                    break;
                }
                catch (ApiException e)
                {
                    _logger.LogError(
                        e,
                        "Cannot link autotest {AutotestId} to work item {WorkItemId}",
                        autotestId,
                        workItemId);

                    Thread.Sleep(WAITING_TIME);
                }
            }
        }
    }

    private static bool IsMissingWorkItem(ApiException ex) =>
        ex.ErrorCode == 404
        || ex.Message.Contains("NotFoundException", StringComparison.OrdinalIgnoreCase);

    public async Task DeleteAutoTestLinkFromWorkItem(string autotestId, string workItemId)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug(
                "Unlink autotest {AutotestId} from workitem {WorkitemId}",
                autotestId,
                workItemId);
        }

        for (var attempts = 0; attempts < MAX_TRIES; attempts++)
        {
            try
            {
                await _autoTests.ApiAdaptersAutoTestsIdWorkItemsDeleteAsync(autotestId, workItemId).ConfigureAwait(false);
                _logger.LogDebug(
                    "Unlink autotest {AutotestId} from workitem {WorkitemId} is successfully",
                    autotestId,
                    workItemId);

                return;
            }
            catch (ApiException e)
            {
                _logger.LogError(
                    e,
                    "Cannot link autotest {AutotestId} to work item {WorkitemId}",
                    autotestId,
                    workItemId);

                Thread.Sleep(WAITING_TIME);
            }
        }
    }

    public async Task<List<AutoTestWorkItemIdentifierApiResult>> GetWorkItemsLinkedToAutoTest(string autotestId)
    {
        return await _autoTests.ApiAdaptersAutoTestsIdWorkItemsGetAsync(autotestId).ConfigureAwait(false);
    }

    public async Task SubmitTestCaseResult(TestContainer result, ClassContainer container)
    {
        _logger.LogDebug("Submitting test result {@Result} to test run {Id}", result, _settings.TestRunId);

        var model = Converter.ConvertResultToModel(result, container, _settings.ConfigurationId);
        HtmlEscapeUtils.EscapeHtmlInObject(model);

        var testRunId = new Guid(_settings.TestRunId);

        if (model.StatusType == TestStatusType.InProgress)
        {
            await _testRuns.ApiAdaptersTestRunsIdTestResultsPostAsync(testRunId, [model]).ConfigureAwait(false);
            _logger.LogDebug("Submitted InProgress test result to test run {Id} for {ExternalId}", _settings.TestRunId, result.ExternalId);
            return;
        }

        var existing = await FindTestResultByExternalIdAsync(result.ExternalId!).ConfigureAwait(false);

        if (existing != null && !ShouldSendFinalResult(model, existing))
        {
            var update = Converter.ConvertResultToUpdateModel(model);
            HtmlEscapeUtils.EscapeHtmlInObject(update);
            await _testResults.ApiAdaptersTestResultsIdPutAsync(existing.Id, update).ConfigureAwait(false);
            _logger.LogDebug("Updated existing test result {TestResultId} for {ExternalId}", existing.Id, result.ExternalId);
            return;
        }

        await _testRuns.ApiAdaptersTestRunsIdTestResultsPostAsync(testRunId, [model]).ConfigureAwait(false);
        _logger.LogDebug("Submitted test result to test run {Id} for {ExternalId}", _settings.TestRunId, result.ExternalId);
    }

    private async Task<TestResultShortResponse?> FindTestResultByExternalIdAsync(string externalId)
    {
        var filter = new TestResultsFilterApiModel
        {
            TestRunIds = [new Guid(_settings.TestRunId)],
            ConfigurationIds = [new Guid(_settings.ConfigurationId)],
        };

        var results = await _testResults.ApiAdaptersTestResultsSearchPostAsync(0, 100, null!, null!, null!, filter)
            .ConfigureAwait(false);

        return results.FirstOrDefault(r => r.AutotestExternalId == externalId);
    }

    private static bool ShouldSendFinalResult(AutoTestResultsForTestRunModel model, TestResultShortResponse existing) =>
        IsInProgress(existing) && model.StatusType != TestStatusType.InProgress;

    private static bool IsInProgress(TestResultShortResponse result) =>
        result.Status?.Type == TestStatusApiType.InProgress;

    public async Task<string> UploadAttachment(string fileName, Stream content)
    {
        _logger.LogDebug("Uploading attachment {Name}", fileName);

        var response = await _attachments.ApiAdaptersAttachmentsPostAsync(
            new FileParameter(
                filename: Path.GetFileName(fileName),
                content: content,
                contentType: MimeTypes.GetMimeType(fileName))
        ).ConfigureAwait(false);

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

        var createEmptyTestRunApiModel = new CreateEmptyTestRunApiModel
        {
            ProjectId = new Guid(_settings.ProjectId),
            Name = (string.IsNullOrEmpty(_settings.TestRunName) ? null : _settings.TestRunName)!
        };
        
        HtmlEscapeUtils.EscapeHtmlInObject(createEmptyTestRunApiModel);
        
        var testRun = await _testRuns.ApiAdaptersTestRunsPostAsync(createEmptyTestRunApiModel).ConfigureAwait(false);

        _settings.TestRunId = testRun.Id.ToString();

        _logger.LogDebug("Test run id : {ID}", _settings.TestRunId);
    }

    public async Task UpdateTestRun()
    {
        _logger.LogDebug("Updating test run");

        if (string.IsNullOrEmpty(_settings.TestRunId) || string.IsNullOrEmpty(_settings.TestRunName))
        {
            return;
        }

        var testRun = await _testRuns.ApiAdaptersTestRunsIdGetAsync(new Guid(_settings.TestRunId)).ConfigureAwait(false);

        if (testRun.Name.Equals(_settings.TestRunName, StringComparison.Ordinal))
        {
            return;
        }

        var updateEmptyTestRunApiModel = new UpdateEmptyTestRunApiModel(name: _settings.TestRunName)
        {
            Id = testRun.Id,
            Attachments = testRun.Attachments.Select(attachment => new AssignAttachmentApiModel(id: attachment.Id)).ToList(),
            Links = testRun.Links.Select(link => new UpdateLinkApiModel(
                id: link.Id,
                title: link.Title,
                url: link.Url,
                description: link.Description,
                type: link.Type)).ToList(),
        };

        HtmlEscapeUtils.EscapeHtmlInObject(updateEmptyTestRunApiModel);

        await _testRuns.ApiAdaptersTestRunsPutAsync(updateEmptyTestRunApiModel).ConfigureAwait(false);

        _logger.LogDebug("Test run updated");
    }

    public async Task CompleteTestRun()
    {
        _logger.LogDebug("Completing test run");

        if (string.IsNullOrEmpty(_settings.TestRunId))
        {
            return;
        }

        var testRun = await _testRuns.ApiAdaptersTestRunsIdGetAsync(new Guid(_settings.TestRunId)).ConfigureAwait(false);

        if (testRun.Status.Type != TestStatusApiType.Succeeded)
        {
            await _testRuns.ApiAdaptersTestRunsIdCompletePostAsync(new Guid(_settings.TestRunId)).ConfigureAwait(false);
        }

        _logger.LogDebug("Complete test run is successfully");
    }

    public async Task<AutoTestApiResult?> GetAutotestByExternalId(string? externalId)
    {
        _logger.LogDebug("Getting autotest by external id {Id}", externalId);

        var externalIds = new List<string>();
        if (externalId != null)
        {
            externalIds = [externalId];
        }
        
        var filter = new  AutoTestSearchApiModel(
            filter: new AutoTestFilterApiModel
            {
                ExternalIds = externalIds,
                ProjectIds = [new(_settings.ProjectId)],
                IsDeleted = false
            },
            includes: new  AutoTestSearchIncludeApiModel()
        );

        HtmlEscapeUtils.EscapeHtmlInObject(filter);

        var autotests = await _autoTests.ApiAdaptersAutoTestsSearchPostAsync(autoTestSearchApiModel: filter).ConfigureAwait(false);
        var autotest = autotests.FirstOrDefault();

        _logger.LogDebug(
            "Get autotest {@Autotest} by external id {Id}",
            autotest,
            externalId);

        return autotest;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    private void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }
        _autoTests.Dispose();
        _testRuns.Dispose();
        _testResults.Dispose();
        _attachments.Dispose();
    }
}
