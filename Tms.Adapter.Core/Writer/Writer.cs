using Microsoft.Extensions.Logging;
using Tms.Adapter.Core.Configurator;
using Tms.Adapter.Core.Client;
using Tms.Adapter.Core.Models;

namespace Tms.Adapter.Core.Writer;

public class Writer : IWriter
{
    private readonly ILogger<Writer> _logger;
    private readonly ITmsClient _client;
    private readonly TmsSettings _tmsSettings;

    public Writer(ILogger<Writer> logger, ITmsClient client, TmsSettings tmsSettings)
    {
        _logger = logger;
        _client = client;
        _tmsSettings = tmsSettings;
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
                    await _client.UpdateAutotest(result.ExternalId, result.Links, result.ExternalKey);
                }
            }
            else
            {
                await _client.CreateAutotest(result, container);
            }

            if (result.WorkItemIds.Count > 0)
            {
                await UpdateTestLinkToWorkItems(result.ExternalId, result.WorkItemIds);
            }

            await _client.SubmitTestCaseResult(result, container);

            _logger.LogDebug("Autotest with ID {ID} successfully written", result.ExternalId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Can not write autotest with ID {ID}", result.ExternalId);
        }
    }

    private async Task UpdateTestLinkToWorkItems(string externalId, List<string> workItemIds)
    {
        var autotest = await _client.GetAutotestByExternalId(externalId);

        if (autotest == null)
        {
            _logger.LogError("Autotest with {ID} not found", externalId);
            return;
        }

        var autotestId = autotest.Id.ToString();

        var linkedWorkItems = await _client.GetWorkItemsLinkedToAutoTest(autotestId);

        foreach (var linkedWorkItem in linkedWorkItems)
        {
            var linkedWorkItemId = linkedWorkItem.GlobalId.ToString();

            if (workItemIds.Contains(linkedWorkItemId))
            {
                workItemIds.Remove(linkedWorkItemId);

                continue;
            }

            if (_tmsSettings.AutomaticUpdationLinksToTestCases)
            {
                await _client.DeleteAutoTestLinkFromWorkItem(autotestId, linkedWorkItemId);
            }
        }

        await _client.LinkAutoTestToWorkItems(autotestId, workItemIds);
    }
}