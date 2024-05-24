using Tms.Adapter.Core.Storage;

namespace Tms.Adapter.CoreTests.Storage;

[TestClass]
public class ResultStorageTests
{
    private const string ObjectId = "123";
    private const string ObjectValue = "321";
    private const string NotExistObjectId = "456";
    private readonly ResultStorage _storage;

    public ResultStorageTests()
    {
        _storage = new ResultStorage();
        _storage.Put(ObjectId, ObjectValue);
    }

    [TestMethod]
    public void GetObjectExist()
    {
        // Act
        var actual = _storage.Get<string>(ObjectId);

        // Assert
        Assert.AreEqual(ObjectValue, actual);
    }

    [TestMethod]
    [ExpectedException(typeof(KeyNotFoundException))]
    public void GetObjectNotExist()
    {
        // Act & Assert
        _storage.Get<string>(NotExistObjectId);
    }

    [TestMethod]
    public void PutObjectNotExist()
    {
        // Arrange
        const string expected = "qwerty";

        // Act
        var actual = _storage.Put(NotExistObjectId, expected);

        // Assert
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void PutObjectExist()
    {
        // Arrange
        const string expected = "qwerty";

        // Act
        var actual = _storage.Put(ObjectId, expected);

        // Assert
        Assert.AreNotEqual(expected, actual);
        Assert.AreEqual(ObjectValue, actual);
    }

    [TestMethod]
    public void RemoveObjectNotExist()
    {
        // Act
        var actual = _storage.Remove<string>(NotExistObjectId);

        // Assert
        Assert.IsNull(actual);
    }

    [TestMethod]
    public void RemoveObjectExist()
    {
        // Act
        var actual = _storage.Remove<string>(ObjectId);

        // Assert
        Assert.AreEqual(ObjectValue, actual);
    }
}