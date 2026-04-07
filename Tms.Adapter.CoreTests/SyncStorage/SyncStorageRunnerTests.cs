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

        var cut = SyncStorageRunner.ToTestResultCutModel(container);

        Assert.AreEqual("ext-1", cut.AutoTestExternalId);
        Assert.AreEqual("Passed", cut.StatusCode);
        Assert.IsNotNull(cut.StartedOn);
    }
}

#pragma warning restore CA1707
