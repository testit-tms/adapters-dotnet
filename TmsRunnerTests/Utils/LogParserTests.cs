using Tms.Adapter.Models;
using TmsRunner.Utils;

namespace TmsRunnerTests.Utils;

[TestClass]
public class LogParserTests
{
    private const int ValueCount = 2;
    private const string MessageValue = "Some message";

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

    [TestMethod]
    public void GetParametersTraceIsEmpty()
    {
        // Act
        var parameters = LogParser.GetParameters(string.Empty);

        // Assert
        Assert.IsNull(parameters);
    }

    [TestMethod]
    public void GetParametersTraceWithParameters()
    {
        // Arrange
        const string key01 = "testType";
        const string value01 = "Simple";
        const string key02 = "secondParam";
        const string value02 = "123";

        // Act
        var parameters = LogParser.GetParameters(Message);

        // Assert
        Assert.IsNotNull(parameters);
        Assert.AreEqual(ValueCount, parameters.Count);
        Assert.AreEqual(value01, parameters[key01]);
        Assert.AreEqual(value02, parameters[key02]);
    }

    [TestMethod]
    public void GetParametersTraceWithoutParameters()
    {
        // Arrange
        const string trace = "some trace";

        // Act
        var actual = LogParser.GetParameters(trace);

        // Assert
        Assert.IsNull(actual);
    }

    [TestMethod]
    public void GetMessageTraceIsEmpty()
    {
        // Act
        var actual = LogParser.GetMessage(string.Empty);

        // Assert
        Assert.AreEqual(string.Empty, actual);
    }

    [TestMethod]
    public void GetMessageTraceWithMessage()
    {
        // Act
        var actual = LogParser.GetMessage(Message);

        // Assert
        Assert.AreEqual(MessageValue, actual);
    }

    [TestMethod]
    public void GetMessageTraceWithoutMessage()
    {
        // Arrange
        const string trace = "some trace";

        // Act
        var actual = LogParser.GetMessage(trace);

        // Assert
        Assert.AreEqual(string.Empty, actual);
    }

    [TestMethod]
    public void GetLinksTraceIsEmpty()
    {
        // Act
        var actual = LogParser.GetLinks(string.Empty);

        // Assert
        Assert.IsNull(actual);
    }

    [TestMethod]
    public void GetLinksTraceWithLinks()
    {
        // Arrange
        var expect1 = new Link
        {
            Title = "Test 1",
            Url = "https://test.example/",
            Description = "Desc 1",
            Type = LinkType.Issue
        };
        var expect2 = new Link
        {
            Title = "Test 2",
            Url = "https://test2.example/",
            Description = "Desc 2",
            Type = LinkType.Defect
        };

        // Act
        var actual = LogParser.GetLinks(Message);

        // Assert
        Assert.IsNotNull(actual);
        Assert.AreEqual(ValueCount, actual.Count);
        Assert.AreEqual(expect1, actual[0]);
        Assert.AreEqual(expect2, actual[1]);
    }

    [TestMethod]
    public void GetLinksTraceWithoutLinks()
    {
        // Arrange
        const string trace = "some trace";

        // Act
        var actual = LogParser.GetLinks(trace);

        // Assert
        Assert.IsNull(actual);
    }
}