using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tms.Adapter.Core.Models;
using Tms.Adapter.Core.SyncStorage;

namespace Tms.Adapter.CoreTests.SyncStorage;

#pragma warning disable CA1707 // Test names use underscores for readability

[TestClass]
public class SyncStorageRunnerTests
{
    [TestMethod]
    public void ToTestResultCutModel_MapsFields()
    {
        var container = new TestContainer
        {
            ExternalId = "ext-1",
            Status = Status.Passed,
            Start = 1_700_000_000_000
        };

        const string projectId = "019581fa-3d2d-7682-b587-fcd508d42b9f";
        var cut = SyncStorageRunner.ToTestResultCutModel(container, projectId);

        Assert.AreEqual(projectId, cut.ProjectId);
        Assert.AreEqual("ext-1", cut.AutoTestExternalId);
        Assert.AreEqual("Passed", cut.StatusCode);
        Assert.AreEqual("Succeeded", cut.StatusType);
        Assert.IsNotNull(cut.StartedOn);
    }

    [TestMethod]
    public void VersionConstraint_Parse_NumericAndTmsChannel()
    {
        var numeric = SyncStorageVersionConstraint.Parse("v0.4.3");
        Assert.AreEqual("v0.4.3", numeric.NormalizedTag);
        Assert.IsFalse(numeric.HasTmsChannel);

        var tms = SyncStorageVersionConstraint.Parse("0.4.3-tms-6.0");
        Assert.AreEqual("v0.4.3-tms-6.0", tms.NormalizedTag);
        Assert.IsTrue(tms.HasTmsChannel);
        Assert.AreEqual("6.0", tms.TmsChannel);
    }

    [TestMethod]
    public void ReleaseResolver_SelectLatest_PicksNewestOnSameChannel()
    {
        var floor = SyncStorageVersionConstraint.Parse("v0.4.2");
        var tags = new[]
        {
            "v0.4.1",
            "v0.4.2",
            "v0.4.3",
            "v0.4.3-tms-6.0",
            "v0.3.12",
            "v0.5.0"
        };

        var selected = SyncStorageReleaseResolver.SelectLatest(floor, tags);
        Assert.AreEqual("v0.4.3", selected);
    }

    [TestMethod]
    public void ReleaseResolver_SelectLatest_RespectsTmsChannel()
    {
        var floor = SyncStorageVersionConstraint.Parse("v0.4.2-tms-5.8");
        var tags = new[]
        {
            "v0.4.2",
            "v0.4.2-tms-5.8",
            "v0.4.3-tms-6.0",
            "v0.4.3-tms-5.8"
        };

        // Channel must match exactly (5.8 vs 6.0).
        var selected = SyncStorageReleaseResolver.SelectLatest(floor, tags);
        Assert.AreEqual("v0.4.3-tms-5.8", selected);
    }
}

#pragma warning restore CA1707
