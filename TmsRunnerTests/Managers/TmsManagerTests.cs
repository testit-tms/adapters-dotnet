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

    private static readonly Guid TestRunId = Guid.NewGuid();
    private static readonly Guid ConfigurationId = Guid.NewGuid();

    [TestMethod]
    public async Task SubmitResultToTestRunAsync_RerunCreatesSeparateAttemptInTestRun()
    {
        var testRunsApi = new Mock<ITestRunsApiAsync>();
        var testResultsApi = new Mock<ITestResultsApiAsync>();
        var manager = CreateManager(testRunsApi, testResultsApi);
        var initialResult = CreateResult(InitialAttemptDuration);

        await manager.SubmitResultToTestRunAsync(
            TestRunId.ToString(),
            initialResult,
            forceInProgressStatus: true).ConfigureAwait(false);
        await manager.SubmitResultToTestRunAsync(TestRunId.ToString(), initialResult).ConfigureAwait(false);
        await manager.SubmitResultToTestRunAsync(
            TestRunId.ToString(),
            CreateResult(RetryAttemptDuration)).ConfigureAwait(false);

        var batches = GetPostedResultBatches(testRunsApi);

        Assert.AreEqual(3, batches.Count);
        Assert.AreEqual(InitialAttemptDuration, batches[1][0].Duration);
        Assert.AreEqual(RetryAttemptDuration, batches[2][0].Duration);
        Assert.AreEqual(0, testResultsApi.Invocations.Count);
    }

    [TestMethod]
    public async Task SubmitResultToTestRunAsync_FinalResultDoesNotLookupExistingResults()
    {
        var testRunsApi = new Mock<ITestRunsApiAsync>();
        var testResultsApi = new Mock<ITestResultsApiAsync>();
        testResultsApi.SetReturnsDefault(Task.FromResult(new List<TestResultShortResponse>()));
        var manager = CreateManager(testRunsApi, testResultsApi);

        await manager.SubmitResultToTestRunAsync(
            TestRunId.ToString(),
            CreateResult(RetryAttemptDuration)).ConfigureAwait(false);

        var batches = GetPostedResultBatches(testRunsApi);

        Assert.AreEqual(1, batches.Count);
        Assert.AreEqual(RetryAttemptDuration, batches[0][0].Duration);
        Assert.AreEqual(0, testResultsApi.Invocations.Count);
    }

    private static TmsManager CreateManager(
        Mock<ITestRunsApiAsync>? testRunsApi = null,
        Mock<ITestResultsApiAsync>? testResultsApi = null) => new(
        Mock.Of<ILogger<TmsManager>>(),
        Mock.Of<IAttachmentsApiAsync>(),
        Mock.Of<IAutoTestsApiAsync>(),
        testRunsApi?.Object ?? Mock.Of<ITestRunsApiAsync>(),
        testResultsApi?.Object ?? Mock.Of<ITestResultsApiAsync>(),
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

    private static List<List<AutoTestResultsForTestRunModel>> GetPostedResultBatches(Mock<ITestRunsApiAsync> testRunsApi) =>
        testRunsApi.Invocations
            .Where(x => x.Method.Name == nameof(ITestRunsApiAsync.AdaptersTestRunsIdTestResultsPostAsync))
            .Select(x => (List<AutoTestResultsForTestRunModel>)x.Arguments[1])
            .ToList();
}

#pragma warning restore CA1707
