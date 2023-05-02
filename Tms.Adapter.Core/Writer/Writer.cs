using Microsoft.Extensions.Logging;
using Tms.Adapter.Core.Client;
using Tms.Adapter.Core.Models;

namespace Tms.Adapter.Core.Writer;

public class Writer : IWriter
{
    private readonly ILogger<Writer> _logger;
    private readonly ITmsClient _client;

    public Writer(ILogger<Writer> logger, ITmsClient client)
    {
        _logger = logger;
        _client = client;
    }

    public void Write(TestResult result)
    {
        _logger.LogDebug("Write autotest {@Autotest}", result);
        try
        {
            var autotest = _client.GetAutotestExist(result.ExternalId);

            if (autotest != null)
            {
                if (result.Status != Status.Failed)
                {
                    _client.UpdateAutotest(result);
                }
                else
                {
                    autotest.Links = result.Links;
                    _client.UpdateAutotest(autotest);
                }
            }
            else
            {
                _client.CreateAutotest(result);
            }

            result.WorkItemIds.ForEach(id => _client.LinkAutoTestToWorkItem(result.ExternalId, id));

            _logger.LogDebug("Autotest with ID {ID} successfully written", result.ExternalId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Can not write autotest with ID {ID}", result.ExternalId);
        }
    }
}