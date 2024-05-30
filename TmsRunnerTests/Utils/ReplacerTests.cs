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
    public void ReplaceParametersValueIsNull()
    {
        // Arrange
        var replacer = new Replacer();

        // Act
        var actual = replacer.ReplaceParameters(null, ParametersWithValue);

        // Assert
        Assert.IsNull(actual);
    }

    [TestMethod]
    public void ReplaceParametersValueIsEmptyString()
    {
        // Arrange
        var replacer = new Replacer();

        // Act
        var actual = replacer.ReplaceParameters(string.Empty, ParametersWithValue);

        // Assert
        Assert.AreEqual(string.Empty, actual);
    }

    [TestMethod]
    public void ReplaceParametersParametersIsEmpty()
    {
        // Arrange
        var replacer = new Replacer();
        var parameters = new Dictionary<string, string>();

        // Act
        var actual = replacer.ReplaceParameters(ValueWithTags, parameters);

        // Assert
        Assert.AreEqual(ValueWithTags, actual);
    }

    [TestMethod]
    public void ReplaceParametersReplaceTags()
    {
        // Arrange
        var replacer = new Replacer();
        const string excepted = "some string first second third";

        // Act
        var actual = replacer.ReplaceParameters(ValueWithTags, ParametersWithValue);

        // Assert
        Assert.AreEqual(excepted, actual);
    }

    [TestMethod]
    public void ReplaceParametersValueWithOutTags()
    {
        // Arrange
        var replacer = new Replacer();
        const string excepted = "some string first second third";

        // Act
        var actual = replacer.ReplaceParameters(excepted, ParametersWithValue);

        // Assert
        Assert.AreEqual(excepted, actual);
    }
}