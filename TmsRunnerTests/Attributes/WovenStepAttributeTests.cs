using Newtonsoft.Json;

using Tms.Adapter.Attributes;
using Tms.Adapter.Models;

namespace TmsRunnerTests.Attributes;

[TestClass]
[DoNotParallelize]
public class WovenStepAttributeTests
{
    [TestMethod]
    public async Task WovenAsyncStepsKeepHierarchyAndWriteOneResult()
    {
        const int expectedResultCountBeforeCompletion = 0;
        const int expectedStepCount = 2;
        const int expectedResultCount = 2;

        var original = Console.Out;
        await using var writer = new StringWriter();
        try
        {
            Console.SetOut(writer);
            var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            var task = OuterAsync(gate.Task);
            Assert.AreEqual(
                expectedResultCountBeforeCompletion,
                ParseLines<StepResult>(writer, MessageType.TmsStepResult).Count);

            gate.SetResult();
            Assert.AreEqual(TestConstants.AsyncResult, await task.ConfigureAwait(false));

            var steps = ParseLines<StepDto>(writer, MessageType.TmsStep);
            var results = ParseLines<StepResult>(writer, MessageType.TmsStepResult);
            Assert.AreEqual(expectedStepCount, steps.Count);
            Assert.AreEqual(steps[0].Guid, steps[1].ParentGuid);
            Assert.AreEqual(expectedResultCount, results.Count);
            Assert.IsTrue(results.All(x => x.Outcome == TestConstants.PassedOutcome));

            var attachment = ParseLines<Tms.Adapter.Models.File>(writer, MessageType.TmsStepAttachmentAsText).Single();
            Assert.AreEqual(steps[1].Guid, attachment.StepGuid);
        }
        finally
        {
            Console.SetOut(original);
        }
    }

    [TestMethod]
    public async Task WovenFailedAsyncStepWritesOnlyFailedResult()
    {
        const int expectedResultCount = 1;

        var original = Console.Out;
        await using var writer = new StringWriter();
        try
        {
            Console.SetOut(writer);

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(FailAsync).ConfigureAwait(false);

            var results = ParseLines<StepResult>(writer, MessageType.TmsStepResult);
            Assert.AreEqual(expectedResultCount, results.Count);
            Assert.AreEqual(TestConstants.FailedOutcome, results[0].Outcome);
        }
        finally
        {
            Console.SetOut(original);
        }
    }

    [TestMethod]
    public async Task WovenFixtureStepsPropagateSetupAndTeardownPhases()
    {
        const int stepsPerFixturePhase = 2;
        const int expectedStepCount = 4;

        var original = Console.Out;
        await using var writer = new StringWriter();
        try
        {
            Console.SetOut(writer);

            await SetupAsync().ConfigureAwait(false);
            await TeardownAsync().ConfigureAwait(false);

            var steps = ParseLines<StepDto>(writer, MessageType.TmsStep);
            Assert.AreEqual(expectedStepCount, steps.Count);
            Assert.IsTrue(steps.Take(stepsPerFixturePhase).All(x => x.Phase == CallerMethodType.Setup));
            Assert.IsTrue(steps.Skip(stepsPerFixturePhase).All(x => x.Phase == CallerMethodType.Teardown));
            Assert.AreEqual(steps[0].Guid, steps[1].ParentGuid);
            Assert.AreEqual(steps[2].Guid, steps[3].ParentGuid);
        }
        finally
        {
            Console.SetOut(original);
        }
    }

    [Step]
    private static async Task<int> OuterAsync(Task gate)
    {
        await gate.ConfigureAwait(false);
        return await InnerAsync().ConfigureAwait(false);
    }

    [Step]
    private static async Task<int> InnerAsync()
    {
        await Task.Yield();
        global::Tms.Adapter.Adapter.AddAttachments(
            TestConstants.AttachmentContent,
            TestConstants.AttachmentFileName);
        return TestConstants.AsyncResult;
    }

    [Step]
    private static async Task FailAsync()
    {
        await Task.Yield();
        throw new InvalidOperationException(TestConstants.FailureMessage);
    }

    [Step]
    [FixtureMarkers.TestInitialize]
    private static async Task SetupAsync()
    {
        await Task.Yield();
        await FixtureChildAsync().ConfigureAwait(false);
    }

    [Step]
    [FixtureMarkers.TestCleanup]
    private static async Task TeardownAsync()
    {
        await Task.Yield();
        await FixtureChildAsync().ConfigureAwait(false);
    }

    [Step]
    private static async Task FixtureChildAsync()
    {
        await Task.Yield();
    }

    private static List<T> ParseLines<T>(StringWriter writer, MessageType type)
    {
        var prefix = $"{type}: ";
        return writer.ToString()
            .Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries)
            .Where(line => line.StartsWith(prefix, StringComparison.Ordinal))
            .Select(line => JsonConvert.DeserializeObject<T>(line[prefix.Length..])!)
            .ToList();
    }

    private static class FixtureMarkers
    {
        [AttributeUsage(AttributeTargets.Method)]
        public sealed class TestInitializeAttribute : Attribute
        {
        }

        [AttributeUsage(AttributeTargets.Method)]
        public sealed class TestCleanupAttribute : Attribute
        {
        }
    }
}
