using System.Net;
using TestIT.AdaptersApi.Api;
using TestIT.AdaptersApi.Client;
using TestIT.AdaptersApi.Model;

namespace Tms.Adapter.Core.Client;

/// <summary>
/// TMS 5.8 workaround: GET /adapters/testRuns/{id} omits description/launchSource
/// and returns empty links/attachments. Read via v2 until adapters GET is fixed.
/// </summary>
public static class TestRunQuery
{
    public static async Task<TestRunApiResult> GetByIdAsync(
        TestRunsApi api,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var options = new RequestOptions();
        var accept = ClientUtils.SelectHeaderAccept(new[] { "application/json" });
        if (accept != null)
        {
            options.HeaderParameters.Add("Accept", accept);
        }
        options.PathParameters.Add("id", ClientUtils.ParameterToString(id));

        var auth = api.Configuration.GetApiKeyWithPrefix("Authorization");
        if (!string.IsNullOrEmpty(auth))
        {
            options.HeaderParameters.Add("Authorization", auth);
        }

        var session = api.Configuration.GetApiKeyWithPrefix("session");
        if (!string.IsNullOrEmpty(session))
        {
            options.Cookies.Add(new Cookie("session", session));
        }

        var response = await api.AsynchronousClient
            .GetAsync<TestRunApiResult>("/api/v2/testRuns/{id}", options, api.Configuration, cancellationToken)
            .ConfigureAwait(false);

        if (api.ExceptionFactory != null)
        {
            var exception = api.ExceptionFactory(nameof(GetByIdAsync), response);
            if (exception != null)
            {
                throw exception;
            }
        }

        return response.Data;
    }
}
