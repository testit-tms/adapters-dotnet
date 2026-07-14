using Tms.Adapter.Models;
using TmsRunner.Entities;
using TmsRunner.Services;

namespace TmsRunnerTests.Services;

[TestClass]
public class StepTreeBuilderTests
{
    [TestMethod]
    public void ExplicitHierarchyBuildsThreeNestedLevelsAndSiblings()
    {
        const int expectedRootCount = 1;
        const int expectedRootChildCount = 2;
        const int expectedGrandchildNestingLevel = 3;

        var builder = new StepTreeBuilder();
        var root = NewStep("root", CallerMethodType.Setup);
        var child = NewStep("child", CallerMethodType.Setup, root.Guid);
        var grandchild = NewStep("grandchild", CallerMethodType.Setup, child.Guid);
        var sibling = NewStep("sibling", CallerMethodType.Setup, root.Guid);

        builder.AddStep(root);
        builder.AddStep(child);
        builder.AddStep(grandchild);
        builder.ApplyResult(Result(grandchild, TestConstants.PassedOutcome));
        builder.ApplyResult(Result(child, TestConstants.PassedOutcome));
        builder.AddStep(sibling);

        var roots = builder.Build();

        Assert.AreEqual(expectedRootCount, roots.Count);
        Assert.AreSame(root, roots[0]);
        Assert.AreEqual(expectedRootChildCount, root.Steps.Count);
        Assert.AreSame(child, root.Steps[0]);
        Assert.AreSame(grandchild, child.Steps.Single());
        Assert.AreSame(sibling, root.Steps[1]);
        Assert.AreEqual(expectedGrandchildNestingLevel, grandchild.NestingLevel);
    }

    [TestMethod]
    public void ExplicitHierarchyUsesGuidWhenResultsAreInterleaved()
    {
        const int expectedRootCount = 2;

        var builder = new StepTreeBuilder();
        var firstRoot = NewStep("same-name", CallerMethodType.Setup);
        var secondRoot = NewStep("same-name", CallerMethodType.TestMethod);
        var firstChild = NewStep("child", CallerMethodType.Setup, firstRoot.Guid);
        var secondChild = NewStep("child", CallerMethodType.TestMethod, secondRoot.Guid);

        builder.AddStep(firstRoot);
        builder.AddStep(secondRoot);
        builder.AddStep(firstChild);
        builder.ApplyResult(Result(firstRoot, TestConstants.PassedOutcome));
        builder.AddStep(secondChild);
        builder.ApplyResult(Result(firstChild, TestConstants.FailedOutcome));

        var roots = builder.Build();

        Assert.AreEqual(expectedRootCount, roots.Count);
        Assert.AreSame(firstChild, firstRoot.Steps.Single());
        Assert.AreSame(secondChild, secondRoot.Steps.Single());
        Assert.AreEqual(CallerMethodType.Setup, firstChild.Phase);
        Assert.AreEqual(CallerMethodType.TestMethod, secondChild.Phase);
    }

    [TestMethod]
    public void ChildCanArriveBeforeItsParent()
    {
        var builder = new StepTreeBuilder();
        var parent = NewStep("parent", CallerMethodType.TestMethod);
        var child = NewStep("child", CallerMethodType.TestMethod, parent.Guid);

        builder.AddStep(child);
        builder.AddStep(parent);

        var root = builder.Build().Single();
        Assert.AreSame(parent, root);
        Assert.AreSame(child, root.Steps.Single());
    }

    [TestMethod]
    public void FailedResultIsNotOverwrittenByLatePassedResult()
    {
        const long failedResultDuration = 25;

        var builder = new StepTreeBuilder();
        var step = NewStep("async", CallerMethodType.TestMethod);
        builder.AddStep(step);

        builder.ApplyResult(Result(step, TestConstants.FailedOutcome, failedResultDuration));
        builder.ApplyResult(Result(step, TestConstants.PassedOutcome));

        Assert.AreEqual(TestConstants.FailedOutcome, step.Outcome);
        Assert.AreEqual(failedResultDuration, step.Duration);
    }

    [TestMethod]
    public void AttachmentUsesExplicitStepGuidAfterOtherStepCompletes()
    {
        var builder = new StepTreeBuilder();
        var parent = NewStep("parent", CallerMethodType.TestMethod);
        var child = NewStep("child", CallerMethodType.TestMethod, parent.Guid);
        builder.AddStep(parent);
        builder.AddStep(child);
        builder.ApplyResult(Result(parent, TestConstants.PassedOutcome));

        Assert.AreSame(child, builder.GetAttachmentStep(child.Guid));
        Assert.IsNull(builder.GetAttachmentStep(null));
    }

    [TestMethod]
    public void LegacyMessagesStillUseCallerMethodFallback()
    {
        var builder = new StepTreeBuilder();
        var root = new StepModel
        {
            Guid = Guid.NewGuid(),
            CurrentMethod = "Root",
            CallerMethodType = CallerMethodType.TestMethod
        };
        var child = new StepModel
        {
            Guid = Guid.NewGuid(),
            CurrentMethod = "Child",
            CallerMethod = "Root"
        };

        builder.AddStep(root);
        builder.AddStep(child);

        Assert.AreSame(child, builder.Build().Single().Steps.Single());
    }

    private static StepModel NewStep(string method, CallerMethodType phase, Guid? parentGuid = null) => new()
    {
        Guid = Guid.NewGuid(),
        CurrentMethod = method,
        ProtocolVersion = StepDto.CurrentProtocolVersion,
        ParentGuid = parentGuid,
        Phase = phase,
        CallerMethodType = phase
    };

    private static StepResult Result(
        StepModel step,
        string outcome,
        long duration = TestConstants.DefaultStepDuration) => new()
    {
        Guid = step.Guid,
        Outcome = outcome,
        Duration = duration,
        CompletedOn = DateTime.UtcNow
    };
}
