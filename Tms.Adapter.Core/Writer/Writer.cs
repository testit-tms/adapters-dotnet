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

    public async Task Write(TestContainer result, ClassContainer container)
    {
        _logger.LogDebug("Write autotest {@Autotest}", result);

        try
        {
            var autotest = await _client.IsAutotestExist(result.ExternalId);

            if (autotest)
            {
                if (result.Status != Status.Failed)
                {
                    await _client.UpdateAutotest(result, container);
                }
                else
                {
                    if (result.Links.Count != 0)
                    {
                        await _client.UpdateAutotest(result.ExternalId, result.Links);
                    }
                }
            }
            else
            {
                await _client.CreateAutotest(result, container);
            }

            if (result.WorkItemIds.Count > 0)
            {
                await _client.LinkAutoTestToWorkItems(result.ExternalId, result.WorkItemIds);
            }

            await _client.SubmitTestCaseResult(result, container);

            _logger.LogDebug("Autotest with ID {ID} successfully written", result.ExternalId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Can not write autotest with ID {ID}", result.ExternalId);
        }
    }
}