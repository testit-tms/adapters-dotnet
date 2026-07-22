using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Moq;
using TestIT.AdaptersApi.Api;
using TestIT.AdaptersApi.Model;
using TmsRunner.Entities;
using TmsRunner.Entities.AutoTest;
using TmsRunner.Managers;
using TmsRunner.Services;

namespace TmsRunnerTests.Managers;

#pragma warning disable CA1707

[TestClass]
public class TmsManagerTests
{
    private const string ExternalId = "2009";
    private const long InitialAttemptDuration = 27_032;
    private const long RetryAttemptDuration = 26_539;
    private const long ExpectedAccumulatedDuration = InitialAttemptDuration + RetryAttemptDuration;
    private const long ExpectedDurationAfterZeroLengthRetry = InitialAttemptDuration + 1;
    private const int NoCompletedReruns = 0;
    private const string ConfigurationName = "configuration";
    private const string FailedStatusCode = "FAILED";
    private const string InProgressStatusCode = "IN_PROGRESS";

    private static readonly Guid TestRunId = Guid.NewGuid();
    private static readonly Guid ConfigurationId = Guid.NewGuid();
    private static readonly Guid ExistingResultId = Guid.NewGuid();
    private static readonly Guid StatusId = Guid.NewGuid();

    [DataTestMethod]
    [DataRow(RetryAttemptDuration, ExpectedAccumulatedDuration)]
    [DataRow(0L, ExpectedDurationAfterZeroLengthRetry)]
    public async Task SubmitResultToTestRunAsync_RerunAccumulatesDuration(
        long retryDuration,
        long expectedDuration)
    {
        var testResultsApi = new Mock<ITestResultsApiAsync>();
        var searchResults = new List<TestResultShortResponse>
        {
            CreateExistingResult(TestStatusApiType.InProgress)
        };
        testResultsApi.SetReturnsDefault(Task.FromResult(searchResults));

        var manager = CreateManager(testResultsApi.Object);
        var initialResult = CreateResult(InitialAttemptDuration);

        await manager.SubmitResultToTestRunAsync(
            TestRunId.ToString(),
            initialResult,
            forceInProgressStatus: true).ConfigureAwait(false);
        await manager.SubmitResultToTestRunAsync(TestRunId.ToString(), initialResult).ConfigureAwait(false);

        searchResults.Clear();
        searchResults.Add(CreateExistingResult(TestStatusApiType.Failed));

        await manager.SubmitResultToTestRunAsync(
            TestRunId.ToString(),
            CreateResult(retryDuration)).ConfigureAwait(false);

        var updates = GetUpdates(testResultsApi);

        Assert.AreEqual(1, updates.Count);
        Assert.AreEqual(expectedDuration, updates[0].Duration);
    }

    [TestMethod]
    public async Task SubmitResultToTestRunAsync_ExistingResultAccumulatesWithoutLocalState()
    {
        var testResultsApi = new Mock<ITestResultsApiAsync>();
        testResultsApi.SetReturnsDefault(Task.FromResult(new List<TestResultShortResponse>
        {
            CreateExistingResult(TestStatusApiType.Failed)
        }));
        var manager = CreateManager(testResultsApi.Object);

        await manager.SubmitResultToTestRunAsync(
            TestRunId.ToString(),
            CreateResult(RetryAttemptDuration)).ConfigureAwait(false);

        var updates = GetUpdates(testResultsApi);

        Assert.AreEqual(1, updates.Count);
        Assert.AreEqual(ExpectedAccumulatedDuration, updates[0].Duration);
    }

    private static TmsManager CreateManager(ITestResultsApiAsync testResultsApi) => new(
        Mock.Of<ILogger<TmsManager>>(),
        Mock.Of<IAttachmentsApiAsync>(),
        Mock.Of<IAutoTestsApiAsync>(),
        Mock.Of<ITestRunsApiAsync>(),
        testResultsApi,
        Mock.Of<IProjectsApiAsync>(),
        new TmsSettings
        {
            TestRunId = TestRunId.ToString(),
            ConfigurationId = ConfigurationId.ToString()
        },
        Mock.Of<ITestRunContextService>());

    private static AutoTestResult CreateResult(long duration) => new()
    {
        ExternalId = ExternalId,
        Outcome = TestOutcome.Failed,
        Duration = duration
    };

    private static List<TestResultUpdateRequest> GetUpdates(Mock<ITestResultsApiAsync> testResultsApi) =>
        testResultsApi.Invocations
            .Where(x => x.Method.Name == nameof(ITestResultsApiAsync.AdaptersTestResultsIdPutAsync))
            .Select(x => (TestResultUpdateRequest)x.Arguments[1])
            .ToList();

    private static TestResultShortResponse CreateExistingResult(TestStatusApiType statusType)
    {
        var statusName = statusType == TestStatusApiType.InProgress
            ? TestConstants.InProgressOutcome
            : TestConstants.FailedOutcome;
        var statusCode = statusType == TestStatusApiType.InProgress
            ? InProgressStatusCode
            : FailedStatusCode;

        return new TestResultShortResponse(
            id: ExistingResultId,
            name: ExternalId,
            autotestExternalId: ExternalId,
            autoTestTags: [],
            testRunId: TestRunId,
            configurationId: ConfigurationId,
            configurationName: ConfigurationName,
            outcome: statusName,
            status: new TestStatusApiResult(StatusId, statusType, statusCode),
            resultReasons: [],
            comment: string.Empty,
            duration: InitialAttemptDuration,
            links: [],
            attachments: [],
            rerunCompletedCount: NoCompletedReruns);
    }
}

#pragma warning restore CA1707
