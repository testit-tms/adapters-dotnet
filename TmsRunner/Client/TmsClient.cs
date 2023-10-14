using Serilog;
using TestIt.Client.Api;
using TestIt.Client.Client;
using TestIt.Client.Model;
using TmsRunner.Handlers;
using TmsRunner.Logger;
using TmsRunner.Models;

namespace TmsRunner.Client
{
    public class TmsClient : ITmsClient
    {
        private readonly TmsSettings _settings;
        private readonly ILogger _logger;
        private readonly TestRunsApi _testRuns;
        private readonly AttachmentsApi _attachments;
        private readonly AutoTestsApi _autoTests;
        private readonly ProjectsApi _projectsApi;

        public TmsClient(TmsSettings settings)
        {
            _logger = LoggerFactory.GetLogger().ForContext<TmsClient>();
            _settings = settings;

            var cfg = new TestIt.Client.Client.Configuration { BasePath = settings.Url };
            cfg.AddApiKeyPrefix("Authorization", "PrivateToken");
            cfg.AddApiKey("Authorization", settings.PrivateToken);

            var httpClientHandler = new HttpClientHandler();
            httpClientHandler.ServerCertificateCustomValidationCallback = (_, _, _, _) => _settings.CertValidation;

            _testRuns = new TestRunsApi(new HttpClient(new RetryHandler(new HttpClientHandler())), cfg, httpClientHandler);
            _attachments = new AttachmentsApi(new HttpClient(new RetryHandler(new HttpClientHandler())), cfg, httpClientHandler);
            _autoTests = new AutoTestsApi(new HttpClient(new RetryHandler(new HttpClientHandler())), cfg, httpClientHandler);
            _projectsApi = new ProjectsApi(new HttpClient(new RetryHandler(new HttpClientHandler())), cfg, httpClientHandler);
        }

        public async Task<TestRunV2GetModel> CreateTestRun()
        {
            var createTestRunRequestBody = new TestRunV2PostShortModel
            {
                ProjectId = new Guid(_settings.ProjectId),
                Name = (string.IsNullOrEmpty(_settings.TestRunName) ? null : _settings.TestRunName)!
            };

            _logger.Debug("Creating test run {@TestRun}", createTestRunRequestBody);

            var testRun = await _testRuns.CreateEmptyAsync(createTestRunRequestBody);

            if (testRun is null)
            {
                throw new Exception($"Could not find project with id: {_settings.ProjectId}");
            }

            _logger.Debug("Created test run {@TestRun}", testRun);

            return testRun;
        }

        public async Task<List<string>> GetAutoTestsForRun(string testRunId)
        {
            _logger.Debug("Getting autotests for run from test run {Id}", testRunId);

            var testRun = await _testRuns.GetTestRunByIdAsync(new Guid(testRunId));

            var autotests = testRun.TestResults.Select(x => x.AutoTest.ExternalId).ToList();

            _logger.Debug(
                "Autotests for run from test run {Id}: {@Autotests}",
                testRunId,
                autotests);

            return autotests;
        }

        public async Task SubmitResultToTestRun(TestRunV2GetModel testRun, AutoTestResult result)
        {
            _logger.Debug("Submitting test result {@Result} to test run {@Id}", result, testRun.Id);

            var model = Converter.ConvertResultToModel(result, _settings.ConfigurationId);

            await _testRuns.SetAutoTestResultsForTestRunAsync(testRun.Id,
                    new List<AutoTestResultsForTestRunModel> { model });

            if (_settings.AdapterMode == 0)
            {
                var resultsInTestRun = testRun
                    .TestResults
                    .Where(x => x.AutoTest.ExternalId == result.ExternalId && x.Parameters.Any())
                    .ToList();

                foreach (var resultInTestRun in resultsInTestRun)
                {
                    model.Parameters = resultInTestRun.Parameters;
                    await _testRuns.SetAutoTestResultsForTestRunAsync(testRun.Id,
                        new List<AutoTestResultsForTestRunModel> { model });
                }
            }

            _logger.Debug("Submit test result to test run {Id} is successfully", testRun.Id);
        }

        public async Task<AttachmentModel> UploadAttachment(string fileName, Stream content)
        {
            _logger.Debug("Uploading attachment {Name}", fileName);

            var response = await _attachments.ApiV2AttachmentsPostAsync(
                new FileParameter(
                    filename: Path.GetFileName(fileName),
                    content: content,
                    contentType: MimeTypes.GetMimeType(fileName))
            );

            _logger.Debug("Upload attachment {@Attachment} is successfully", response);

            return response;
        }

        public async Task<AutoTestModel?> GetAutotestByExternalId(string externalId)
        {
            _logger.Debug("Getting autotest by external id {Id}", externalId);

            var filter = new AutotestsSelectModel
            {
                Filter = new AutotestFilterModel
                {
                    ExternalIds = new List<string> { externalId },
                    ProjectIds = new List<Guid> { new Guid(_settings.ProjectId) },
                    IsDeleted = false
                }
            };

            var autotests = await _autoTests.ApiV2AutoTestsSearchPostAsync(autotestsSelectModel: filter);
            var autotest = autotests.FirstOrDefault();

            _logger.Debug(
                "Get autotest {@Autotest} by external id {Id}",
                autotest,
                externalId);

            return autotest;
        }

        public async Task<AutoTestModel> CreateAutotest(AutoTest dto)
        {
            _logger.Debug("Creating autotest {@Autotest}", dto);

            var model = Converter.ConvertAutoTestDtoToPostModel(dto, _settings.ProjectId);
            model.ShouldCreateWorkItem = _settings.AutomaticCreationTestCases;
            var response = await _autoTests.CreateAutoTestAsync(model);

            _logger.Debug("Create autotest {@Autotest} is successfully", response);

            return response;
        }

        public async Task UpdateAutotest(AutoTest dto)
        {
            _logger.Debug("Updating autotest {@Autotest}", dto);

            var model = Converter.ConvertAutoTestDtoToPutModel(dto, _settings.ProjectId);
            await _autoTests.UpdateAutoTestAsync(model);

            _logger.Debug("Update autotest {@Autotest} is successfully", model);
        }

        public async Task<ProjectModel> GetProject()
        {
            return await _projectsApi.GetProjectByIdAsync(_settings.ProjectId);
        }

        public async Task<TestRunV2GetModel> GetTestRun(string id)
        {
            return await _testRuns.GetTestRunByIdAsync(new Guid(id));
        }

        public async Task LinkAutoTestToWorkItem(string autotestId, string workItemId)
        {
            _logger.Debug(
                "Linking autotest {AutotestId} to workitem {WorkitemId}",
                autotestId,
                workItemId);

            try
            {
                await _autoTests.LinkAutoTestToWorkItemAsync(autotestId, new WorkItemIdModel(workItemId));
            }
            catch (ApiException e) when (e.Message.Contains("was not found"))
            {
                _logger.Error(
                    "Cannot link autotest {AutotestId} to workitem {WorkitemId}: workitem was not found",
                    autotestId,
                    workItemId);
                return;
            }

            _logger.Debug(
                "Link autotest {AutotestId} to workitem {WorkitemId} is successfully",
                autotestId,
                workItemId);
        }
    }
}