using System.Reflection;

using MethodBoundaryAspect.Fody.Attributes;
using Newtonsoft.Json;

using Tms.Adapter.Attributes;
using Tms.Adapter.Models;

namespace TmsRunnerTests.Attributes;

[TestClass]
[DoNotParallelize]
public class StepAttributeTests
{
    private static readonly MethodInfo DummyMethod = typeof(StepAttributeTests)
        .GetMethod(nameof(Dummy), BindingFlags.NonPublic | BindingFlags.Static)!;

    [TestMethod]
    public void NestedStepContainsParentGuidAndInheritedPhase()
    {
        const int expectedStepCount = 2;

        CaptureConsole((writer) =>
        {
            var outerAspect = new StepAttribute();
            var innerAspect = new StepAttribute();
            var outerArgs = Args();
            var innerArgs = Args();

            outerAspect.OnEntry(outerArgs);
            innerAspect.OnEntry(innerArgs);
            innerAspect.OnExit(innerArgs);
            outerAspect.OnExit(outerArgs);

            var steps = ParseLines<StepDto>(writer, MessageType.TmsStep);
            Assert.AreEqual(expectedStepCount, steps.Count);
            Assert.AreEqual(StepDto.CurrentProtocolVersion, steps[0].ProtocolVersion);
            Assert.IsNull(steps[0].ParentGuid);
            Assert.AreEqual(steps[0].Guid, steps[1].ParentGuid);
            Assert.AreEqual(steps[0].Phase, steps[1].Phase);
        });
    }

    [TestMethod]
    public async Task AsyncStepWritesResultOnlyAfterTaskCompletes()
    {
        const int expectedResultCountBeforeCompletion = 0;
        const int expectedResultCountAfterCompletion = 1;

        await CaptureConsoleAsync(async writer =>
        {
            var source = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var aspect = new StepAttribute();
            var args = Args();

            aspect.OnEntry(args);
            args.ReturnValue = source.Task;
            aspect.OnExit(args);

            Assert.AreEqual(
                expectedResultCountBeforeCompletion,
                ParseLines<StepResult>(writer, MessageType.TmsStepResult).Count);

            source.SetResult();
            await ((Task)args.ReturnValue).ConfigureAwait(false);

            var results = ParseLines<StepResult>(writer, MessageType.TmsStepResult);
            Assert.AreEqual(expectedResultCountAfterCompletion, results.Count);
            Assert.AreEqual(TestConstants.PassedOutcome, results[0].Outcome);
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FailedAsyncStepWritesSingleFailedResult()
    {
        const int expectedResultCount = 1;

        await CaptureConsoleAsync(async writer =>
        {
            var source = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var aspect = new StepAttribute();
            var args = Args();

            aspect.OnEntry(args);
            args.ReturnValue = source.Task;
            aspect.OnExit(args);
            source.SetException(new InvalidOperationException(TestConstants.FailureMessage));

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await ((Task)args.ReturnValue).ConfigureAwait(false)).ConfigureAwait(false);

            var results = ParseLines<StepResult>(writer, MessageType.TmsStepResult);
            Assert.AreEqual(expectedResultCount, results.Count);
            Assert.AreEqual(TestConstants.FailedOutcome, results[0].Outcome);
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task GenericTaskKeepsItsResult()
    {
        await CaptureConsoleAsync(async writer =>
        {
            var aspect = new StepAttribute();
            var args = Args();
            aspect.OnEntry(args);
            args.ReturnValue = Task.FromResult(TestConstants.AsyncResult);

            aspect.OnExit(args);

            Assert.AreEqual(TestConstants.AsyncResult, await ((Task<int>)args.ReturnValue).ConfigureAwait(false));
            var result = ParseLines<StepResult>(writer, MessageType.TmsStepResult).Single();
            Assert.AreEqual(TestConstants.AsyncResult.ToString(), result.Result);
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public void AttachmentContainsCurrentStepGuid()
    {
        CaptureConsole(writer =>
        {
            var aspect = new StepAttribute();
            var args = Args();
            aspect.OnEntry(args);

            Tms.Adapter.Adapter.AddAttachments(
                TestConstants.AttachmentContent,
                TestConstants.AttachmentFileName);
            aspect.OnExit(args);

            var step = ParseLines<StepDto>(writer, MessageType.TmsStep).Single();
            var attachment = ParseLines<Tms.Adapter.Models.File>(writer, MessageType.TmsStepAttachmentAsText).Single();
            Assert.AreEqual(step.Guid, attachment.StepGuid);
        });
    }

    private static MethodExecutionArgs Args() => new()
    {
        Method = DummyMethod,
        Arguments = []
    };

    private static void Dummy()
    {
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

    private static void CaptureConsole(Action<StringWriter> action)
    {
        var original = Console.Out;
        using var writer = new StringWriter();
        try
        {
            Console.SetOut(writer);
            action(writer);
        }
        finally
        {
            Console.SetOut(original);
        }
    }

    private static async Task CaptureConsoleAsync(Func<StringWriter, Task> action)
    {
        var original = Console.Out;
        await using var writer = new StringWriter();
        try
        {
            Console.SetOut(writer);
            await action(writer).ConfigureAwait(false);
        }
        finally
        {
            Console.SetOut(original);
        }
    }
}
