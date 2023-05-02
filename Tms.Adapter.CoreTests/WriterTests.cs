using Microsoft.Extensions.Logging;
using NSubstitute;
using Tms.Adapter.Core.Client;
using Tms.Adapter.Core.Models;
using Tms.Adapter.Core.Writer;
using TestResult = Tms.Adapter.Core.Models.TestResult;

namespace Tms.Adapter.CoreTests;

[TestClass]
public class WriterTests
{
    private ILogger<Writer> _logger;
    private ITmsClient _client;
    private TestResult _testResult;
    private TestResult _existAutotest;

    private const string ExternalId = "external_id";

    [TestInitialize]
    public void TestSetup()
    {
        _logger = Substitute.For<ILogger<Writer>>();
        _client = Substitute.For<ITmsClient>();

        _existAutotest = new TestResult
        {
            ExternalId = ExternalId,
            Status = Status.Failed,
            Links = new List<Link>(),
            WorkItemIds = new List<string>(),
        };

        _testResult = new TestResult
        {
            ExternalId = ExternalId,
            Status = Status.Failed,
            Links = new List<Link>
            {
                new Link()
                {
                    Title = "Title 1",
                    Description = "Description 1",
                    Type = LinkType.Issue,
                    Url = "https://test.example"
                }
            },
            WorkItemIds = new List<string>(),
        };
    }

    [TestMethod]
    public void Write_AutotestExistAndFailed()
    {
        _client.GetAutotestExist(ExternalId).Returns(_existAutotest);
        _existAutotest.Links = _testResult.Links;
        var writer = new Writer(_logger, _client);

        writer.Write(_testResult);

        _client.Received().UpdateAutotest(_existAutotest);
        _client.DidNotReceive().LinkAutoTestToWorkItem(Arg.Any<string>(), Arg.Any<string>());
    }

    [TestMethod]
    public void Write_AutotestExistAndSuccess()
    {
        _testResult.Status = Status.Passed;
        _client.GetAutotestExist(ExternalId).Returns(_existAutotest);
        var writer = new Writer(_logger, _client);

        writer.Write(_testResult);

        _client.Received().UpdateAutotest(_testResult);
        _client.DidNotReceive().LinkAutoTestToWorkItem(Arg.Any<string>(), Arg.Any<string>());
    }

    [TestMethod]
    public void Write_AutotestNotExist()
    {
        _client.GetAutotestExist(ExternalId).Returns((TestResult?)null);
        var writer = new Writer(_logger, _client);

        writer.Write(_testResult);

        _client.Received().CreateAutotest(_testResult);
        _client.DidNotReceive().LinkAutoTestToWorkItem(Arg.Any<string>(), Arg.Any<string>());
    }

    [TestMethod]
    public void Write_LinkAutoTests()
    {
        const string workItemId = "123";
        _testResult.WorkItemIds = new List<string> { workItemId };
        _client.GetAutotestExist(ExternalId).Returns((TestResult?)null);
        var writer = new Writer(_logger, _client);

        writer.Write(_testResult);

        _client.Received().CreateAutotest(_testResult);
        _client.Received().LinkAutoTestToWorkItem(ExternalId, workItemId);
    }

    [TestMethod]
    public void Write_GetAutotestFailed()
    {
        var exception = new Exception("test exception");
        _client.GetAutotestExist(ExternalId).Returns(x => throw exception);
        var writer = new Writer(_logger, _client);

        writer.Write(_testResult);

        _client.DidNotReceive().CreateAutotest(Arg.Any<TestResult>());
        _client.DidNotReceive().UpdateAutotest(Arg.Any<TestResult>());
        _client.DidNotReceive().LinkAutoTestToWorkItem(Arg.Any<string>(), Arg.Any<string>());
    }

    [TestMethod]
    public void Write_UpdateAutotestFailed()
    {
        var exception = new Exception("test exception");
        _testResult.Status = Status.Passed;
        _client.GetAutotestExist(ExternalId).Returns(_testResult);
        _client.When(x => x.UpdateAutotest(_testResult))
            .Do(x => throw exception);
        var writer = new Writer(_logger, _client);

        writer.Write(_testResult);

        _client.DidNotReceive().CreateAutotest(Arg.Any<TestResult>());
        _client.DidNotReceive().LinkAutoTestToWorkItem(Arg.Any<string>(), Arg.Any<string>());
    }

    [TestMethod]
    public void Write_CreateAutotestFailed()
    {
        var exception = new Exception("test exception");
        _testResult.Status = Status.Passed;
        _client.GetAutotestExist(ExternalId).Returns((TestResult?)null);
        _client.When(x => x.CreateAutotest(_testResult))
            .Do(x => throw exception);
        var writer = new Writer(_logger, _client);

        writer.Write(_testResult);

        _client.DidNotReceive().UpdateAutotest(Arg.Any<TestResult>());
        _client.DidNotReceive().LinkAutoTestToWorkItem(Arg.Any<string>(), Arg.Any<string>());
    }

    [TestMethod]
    public void Write_LinkAutotestFailed()
    {
        const string workItemId = "123";
        var exception = new Exception("test exception");
        _testResult.Status = Status.Passed;
        _client.GetAutotestExist(ExternalId).Returns((TestResult?)null);
        _client.When(x => x.LinkAutoTestToWorkItem(ExternalId, workItemId))
            .Do(x => throw exception);
        var writer = new Writer(_logger, _client);

        writer.Write(_testResult);

        _client.DidNotReceive().UpdateAutotest(Arg.Any<TestResult>());
        _client.Received().CreateAutotest(Arg.Any<TestResult>());
    }
}