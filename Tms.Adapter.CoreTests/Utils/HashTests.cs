using Tms.Adapter.Core.Utils;

namespace Tms.Adapter.CoreTests.Utils;

[TestClass]
public class HashTests
{
    [TestMethod]
    public void GetStringSha256Hash()
    {
        // Arrange
        const string expected = "9F86D081884C7D659A2FEAA0C55AD015A3BF4F1B2B0B822CD15D6C15B0F00A08";
        
        // Act
        var actual = Hash.GetStringSha256Hash("test");

        // Assert
        Assert.AreEqual(expected, actual);
    }
    
    [TestMethod]
    public void GetNewId()
    {
        // Act
        var actual1 = Hash.NewId();
        var actual2 = Hash.NewId();

        // Assert
        Assert.IsFalse(string.IsNullOrWhiteSpace(actual1));
        Assert.IsFalse(string.IsNullOrWhiteSpace(actual2));
        Assert.AreNotEqual(actual1, actual2);
    }
}