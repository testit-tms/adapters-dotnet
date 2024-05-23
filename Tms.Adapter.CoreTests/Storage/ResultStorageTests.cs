using Tms.Adapter.Core.Storage;

namespace Tms.Adapter.CoreTests.Storage;

[TestClass]
public class ResultStorageTests
{
    private readonly ResultStorage _storage;

    private const string ObjectId = "123";
    private const string ObjectValue = "321";
    private const string NotExistObjectId = "456";
    
    public ResultStorageTests()
    {
        _storage = new ResultStorage();
        _storage.Put(ObjectId, ObjectValue);
    }

    [TestMethod]
    public void GetObjectExist()
    {
        var value = _storage.Get<string>(ObjectId);

        Assert.AreEqual(ObjectValue, value);
    }

    [TestMethod]
    [ExpectedException(typeof(KeyNotFoundException))]
    public void GetObjectNotExist()
    {
        _storage.Get<string>(NotExistObjectId);
    }

    [TestMethod]
    public void PutObjectNotExist()
    {
        const string newValue = "qwerty";

        var value = _storage.Put(NotExistObjectId, newValue);

        Assert.AreEqual(newValue, value);
    }

    [TestMethod]
    public void PutObjectExist()
    {
        const string newValue = "qwerty";

        var value = _storage.Put(ObjectId, newValue);

        Assert.AreNotEqual(newValue, value);
        Assert.AreEqual(ObjectValue, value);
    }

    [TestMethod]
    public void RemoveObjectNotExist()
    {
        var value = _storage.Remove<string>(NotExistObjectId);

        Assert.IsNull(value);
    }

    [TestMethod]
    public void RemoveObjectExist()
    {
        var value = _storage.Remove<string>(ObjectId);

        Assert.AreEqual(ObjectValue, value);
    }
}