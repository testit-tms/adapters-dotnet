using TmsRunner.Extensions;

namespace TmsRunnerTests.Extensions;

[TestClass]
public class StringExtensionTests
{
    [TestMethod]
    [DataRow("", "test")]
    [DataRow(" ", "test")]
    [DataRow(null, "test")]
    [DataRow("input", "input")]
    public void AssignIfNullOrEmpty(string input, string expected)
    {
        // Arrange
        const string replaceValue = "test";

        // Act
        var actual = replaceValue.AssignIfNullOrEmpty(input);

        // Assert
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void ComputeHash()
    {
        // Arrange
        const string input = "test";
        const string expected = "2177926815";

        // Act
        var actual = input.ComputeHash();

        // Assert
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void RemoveQuotes()
    {
        // Arrange
        const string input = "'test\"";
        const string expected = "test";

        // Act
        var actual = input.RemoveQuotes();

        // Assert
        Assert.AreEqual(expected, actual);
    }
}