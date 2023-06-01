using Tms.Adapter.Core.Storage;

namespace Tms.Adapter.CoreTests;

[TestClass]
public class ResultStorageTests
{
    private ResultStorage _storage;

    private const string ObjectId = "123";
    private const string ObjectValue = "321";
    private const string NotExistObjectId = "456";

    [TestInitialize]
    public void TestSetup()
    {
        _storage = new ResultStorage();
        _storage.Put(ObjectId, ObjectValue);
    }

    [TestMethod]
    public void Get_ObjectExist()
    {
        var value = _storage.Get<string>(ObjectId);

        Assert.AreEqual(ObjectValue, value);
    }

    [TestMethod]
    [ExpectedException(typeof(KeyNotFoundException))]
    public void Get_ObjectNotExist()
    {
        _storage.Get<string>(NotExistObjectId);
    }

    [TestMethod]
    public void Put_ObjectNotExist()
    {
        const string newValue = "qwerty";

        var value = _storage.Put(NotExistObjectId, newValue);

        Assert.AreEqual(newValue, value);
    }

    [TestMethod]
    public void Put_ObjectExist()
    {
        const string newValue = "qwerty";

        var value = _storage.Put(ObjectId, newValue);

        Assert.AreNotEqual(newValue, value);
        Assert.AreEqual(ObjectValue, value);
    }

    [TestMethod]
    public void Remove_ObjectNotExist()
    {
        var value = _storage.Remove<string>(NotExistObjectId);

        Assert.IsNull(value);
    }

    [TestMethod]
    public void Remove_ObjectExist()
    {
        var value = _storage.Remove<string>(ObjectId);

        Assert.AreEqual(ObjectValue, value);
    }
}