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
}

#pragma warning restore CA1707
