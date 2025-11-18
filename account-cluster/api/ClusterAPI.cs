using XYVR.API.Audit;
using XYVR.Core;

namespace XYVR.AccountAuthority.Cluster.api;

internal class ClusterAPI
{
    private const string VRChatApiSourceName = "cluster_web_api";
    
    private readonly IResponseCollector _responseCollector;
    private readonly CancellationTokenSource _cancellationTokenSource;
    
    private readonly HttpClient _client;
    
    public ClusterAPI(IResponseCollector responseCollector, CancellationTokenSource cancellationTokenSource)
    {
        _responseCollector = responseCollector;
        _cancellationTokenSource = cancellationTokenSource;
        
        _client = new HttpClient();
        _client.DefaultRequestHeaders.UserAgent.ParseAdd(XYVRValues.UserAgent);
    }

    public async Task<string> GetFriends(DataCollectionReason dataCollectionReason, int pageSize = 30)
    {
        var bearer = "TODO_BEARER_TOKEN";
        
        string UrlBuilder(int page) => $"{AuditUrls.ClusterApiUrlV1}/user_friends_all?page_Sze={pageSize}";

        var url = UrlBuilder(pageSize);
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Bearer", bearer);
        var requestGuid = XYVRGuids.ForRequest();

        // TODO: Pagination using "next" contained in the response
        var response = await _client.SendAsync(request, _cancellationTokenSource.Token);
        EnsureSuccessOrThrowVerbose(response);

        var responseStr = await response.Content.ReadAsStringAsync();
        DataCollectSuccess(url, requestGuid, responseStr, dataCollectionReason);
        
        // TODO: Deserialize this
        return responseStr;
    }

    private static void EnsureSuccessOrThrowVerbose(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Request failed with status {response.StatusCode}, reason: {response.ReasonPhrase}");
        }
    }
    
    private void DataCollectSuccess(string url, string requestGuid, string responseStr, DataCollectionReason dataCollectionReason)
    {
        _responseCollector.Ingest(new ResponseCollectionTrail
        {
            timestamp = _responseCollector.GetCurrentTime(),
            trailGuid = XYVRGuids.ForTrail(),
            requestGuid = requestGuid,
            reason = dataCollectionReason,
            apiSource = VRChatApiSourceName,
            route = url,
            status = DataCollectionResponseStatus.Success,
            responseObject = responseStr,
            metaObject = null,
        });
    }
}