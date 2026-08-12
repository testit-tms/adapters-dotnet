using TestIT.AdaptersApi.Model;
using Tms.Adapter.Core.Models;
using Tms.Adapter.Core.Utils;
using ApiLinkType = TestIT.AdaptersApi.Model.LinkType;

namespace Tms.Adapter.CoreTests.Utils;

[TestClass]
public class TestRunMetadataTests
{
    [TestMethod]
    public void ParseTags_CommaSeparated()
    {
        var tags = TestRunMetadata.ParseTags("smoke, nightly, smoke");

        CollectionAssert.AreEqual(new[] { "smoke", "nightly" }, tags);
    }

    [TestMethod]
    public void ParseTags_JsonArray()
    {
        var tags = TestRunMetadata.ParseTags("[\"smoke\", \"nightly\"]");

        CollectionAssert.AreEqual(new[] { "smoke", "nightly" }, tags);
    }

    [TestMethod]
    public void ParseTags_InvalidJson_ReturnsEmpty()
    {
        var tags = TestRunMetadata.ParseTags("[invalid");

        Assert.AreEqual(0, tags.Count);
    }

    [TestMethod]
    public void ParseLinks_RequiresUrl()
    {
        var links = TestRunMetadata.ParseLinks(
            """[{"url":"https://ci/job/1","title":"CI","type":"Related"},{"title":"no-url"},{"url":" https://ci/job/2 "}]""");

        Assert.AreEqual(2, links.Count);
        Assert.AreEqual("https://ci/job/1", links[0].Url);
        Assert.AreEqual("CI", links[0].Title);
        Assert.AreEqual("https://ci/job/2", links[1].Url);
    }

    [TestMethod]
    public void MergeTags_KeepsExistingAndAddsNew()
    {
        var merged = TestRunMetadata.MergeTags(["ui", "smoke"], ["smoke", "nightly"]);

        CollectionAssert.AreEqual(new[] { "ui", "smoke", "nightly" }, merged);
    }

    [TestMethod]
    public void MergeLinks_DedupesByUrl_CaseSensitive()
    {
        var existing = new List<LinkApiResult>
        {
            new(url: "https://ci/job/1", type: ApiLinkType.Related) { Id = Guid.NewGuid(), Title = "Old" }
        };
        var configured = new List<TestRunLinkConfig>
        {
            new() { Url = "https://ci/job/1", Title = "New" },
            new() { Url = "https://ci/job/2", Type = "Issue" },
            new() { Url = "HTTPS://ci/job/1", Title = "Different case" }
        };

        var merged = TestRunMetadata.MergeLinks(existing, configured);

        Assert.AreEqual(3, merged.Count);
        Assert.AreEqual("Old", merged[0].Title);
        Assert.IsNotNull(merged[0].Id);
        Assert.AreEqual("https://ci/job/2", merged[1].Url);
        Assert.AreEqual(ApiLinkType.Issue, merged[1].Type);
        Assert.AreEqual("HTTPS://ci/job/1", merged[2].Url);
    }

    [TestMethod]
    public void ParseLinkType_Invalid_DefaultsToRelated()
    {
        Assert.AreEqual(ApiLinkType.Related, TestRunMetadata.ParseLinkType(null));
        Assert.AreEqual(ApiLinkType.Related, TestRunMetadata.ParseLinkType("Unknown"));
        Assert.AreEqual(ApiLinkType.Defect, TestRunMetadata.ParseLinkType("defect"));
    }
}
