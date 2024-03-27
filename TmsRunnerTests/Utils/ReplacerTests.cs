using Tms.Adapter.Utils;

namespace TmsRunnerTests.Utils;

[TestClass]
public class ReplacerTests
{
    private const string ValueWithTags = "some string {a} {b} {c}";

    private static readonly Dictionary<string, string> ParametersWithValue = new()
    {
        { "a", "first" },
        { "b", "second" },
        { "c", "third" }
    };

    [TestMethod]
    public void ReplaceParameters_ValueIsNull()
    {
        var replacer = new Replacer();

        var value = replacer.ReplaceParameters(null, ParametersWithValue);

        Assert.IsNull(value);
    }

    [TestMethod]
    public void ReplaceParameters_ValueIsEmptyString()
    {
        var replacer = new Replacer();

        var value = replacer.ReplaceParameters(string.Empty, ParametersWithValue);

        Assert.AreEqual(string.Empty, value);
    }

    [TestMethod]
    public void ReplaceParameters_ParametersIsEmpty()
    {
        var replacer = new Replacer();
        var parameters = new Dictionary<string, string>();

        var value = replacer.ReplaceParameters(ValueWithTags, parameters);

        Assert.AreEqual(ValueWithTags, value);
    }

    [TestMethod]
    public void ReplaceParameters_ReplaceTags()
    {
        var replacer = new Replacer();
        const string exceptedValue = "some string first second third";

        var value = replacer.ReplaceParameters(ValueWithTags, ParametersWithValue);

        Assert.AreEqual(exceptedValue, value);
    }

    [TestMethod]
    public void ReplaceParameters_ValueWithOutTags()
    {
        var replacer = new Replacer();
        const string exceptedValue = "some string first second third";

        var value = replacer.ReplaceParameters(exceptedValue, ParametersWithValue);

        Assert.AreEqual(exceptedValue, value);
    }
}