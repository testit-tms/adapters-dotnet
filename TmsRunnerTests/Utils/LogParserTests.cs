using Tms.Adapter.Models;
using TmsRunner.Utils;

namespace TmsRunnerTests.Utils;

[TestClass]
public class LogParserTests
{
    private static readonly string Message =
        MessageType.TmsParameters + ": {\"testType\":\"Simple\", \"secondParam\":\"123\"}\n" +
        MessageType.TmsStep +
        ": {\"Guid\":\"5ebaef93-cc90-440e-adf9-d23a95a3b328\",\"StartedOn\":\"2023-03-28T10:26:53.269419Z\",\"CompletedOn\":null,\"Duration\":0,\"Title\":\"TestCleanup\",\"Description\":null,\"Instance\":\"SumTests\",\"CurrentMethod\":\"TestCleanup\",\"CallerMethod\":null,\"Args\":{},\"Result\":null,\"Steps\":[],\"ParentStep\":null,\"NestingLevel\":0,\"CallerMethodType\":2,\"CurrentMethodType\":2,\"Links\":[],\"Attachments\":[],\"Outcome\":null}\n" +
        MessageType.TmsStepResult +
        ": {\"Guid\":\"5ebaef93-cc90-440e-adf9-d23a95a3b328\",\"CompletedOn\":\"2023-03-28T10:26:53.289941Z\",\"Duration\":0,\"Result\":null,\"Outcome\":\"Passed\"}\n" +
        MessageType.TmsStepMessage + $": {MessageValue}\n" +
        MessageType.TmsStepLinks +
        ": [{\"Type\":4,\"Title\":\"Test 1\",\"Url\":\"https://test.example/\",\"Description\":\"Desc 1\",\"HasInfo\":false}]\n" +
        MessageType.TmsStepLinks +
        ": [{\"Type\":3,\"Title\":\"Test 2\",\"Url\":\"https://test2.example/\",\"Description\":\"Desc 2\",\"HasInfo\":false}]\n";

    private const int ValueCount = 2;
    private const string MessageValue = "Some message";

    [TestMethod]
    public void GetParameters_TraceIsEmpty()
    {
        var parameters = LogParser.GetParameters(string.Empty);

        Assert.IsNull(parameters);
    }

    [TestMethod]
    public void GetParameters_TraceWithParameters()
    {
        const string key01 = "testType";
        const string value01 = "Simple";
        const string key02 = "secondParam";
        const string value02 = "123";

        var parameters = LogParser.GetParameters(Message);

        Assert.IsNotNull(parameters);
        Assert.AreEqual(ValueCount, parameters.Count);
        Assert.AreEqual(value01, parameters[key01]);
        Assert.AreEqual(value02, parameters[key02]);
    }

    [TestMethod]
    public void GetParameters_TraceWithoutParameters()
    {
        const string trace = "some trace";

        var parameters = LogParser.GetParameters(trace);

        Assert.IsNull(parameters);
    }

    [TestMethod]
    public void GetMessage_TraceIsEmpty()
    {
        var result = LogParser.GetMessage(string.Empty);

        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void GetMessage_TraceWithMessage()
    {
        var result = LogParser.GetMessage(Message);

        Assert.AreEqual(MessageValue, result);
    }

    [TestMethod]
    public void GetMessage_TraceWithoutMessage()
    {
        const string trace = "some trace";

        var result = LogParser.GetMessage(trace);

        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void GetLinks_TraceIsEmpty()
    {
        var result = LogParser.GetLinks(string.Empty);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetLinks_TraceWithLinks()
    {
        var expectLink1 = new Link
        {
            Title = "Test 1",
            Url = "https://test.example/",
            Description = "Desc 1",
            Type = LinkType.Issue
        };

        var expectLink2 = new Link
        {
            Title = "Test 2",
            Url = "https://test2.example/",
            Description = "Desc 2",
            Type = LinkType.Defect
        };

        var result = LogParser.GetLinks(Message);

        Assert.IsNotNull(result);
        Assert.AreEqual(ValueCount, result.Count);
        Assert.AreEqual(expectLink1, result[0]);
        Assert.AreEqual(expectLink2, result[1]);
    }

    [TestMethod]
    public void GetLinks_TraceWithoutLinks()
    {
        const string trace = "some trace";

        var result = LogParser.GetLinks(trace);

        Assert.IsNull(result);
    }
}