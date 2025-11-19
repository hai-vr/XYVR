using Newtonsoft.Json;
using XYVR.API.Audit;
using XYVR.Core;

namespace XYVR.AccountAuthority.Cluster;

internal class ClusterAPI
{
    private const string VRChatApiSourceName = "cluster_web_api";
    
    private readonly IResponseCollector _responseCollector;
    private readonly CancellationTokenSource _cancellationTokenSource;
    
    private readonly HttpClient _client;
    
    private string _bearer__sensitive;

    public ClusterAPI(IResponseCollector responseCollector, CancellationTokenSource cancellationTokenSource)
    {
        _responseCollector = responseCollector;
        _cancellationTokenSource = cancellationTokenSource;
        
        _client = new HttpClient();
        _client.DefaultRequestHeaders.UserAgent.ParseAdd(XYVRValues.UserAgent);
        _client.DefaultRequestHeaders.Add("x-cluster-device", "Web");
        _client.DefaultRequestHeaders.Add("x-cluster-platform", "Web");
        _client.DefaultRequestHeaders.Add("x-cluster-app-version", "3.61.2511121816");
        _client.DefaultRequestHeaders.Add("x-cluster-build-version", "2511181137");
    }

    public async Task<ImmutableNonIndexedAccount> GetCallerAccount(DataCollectionReason dataCollectionReason)
    {
        var url = $"{AuditUrls.ClusterApiUrlV1}/login";
        
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Bearer", _bearer__sensitive);
        var requestGuid = XYVRGuids.ForRequest();

        var response = await _client.SendAsync(request, _cancellationTokenSource.Token);
        EnsureSuccessOrThrowVerbose(response);

        var responseStr = await response.Content.ReadAsStringAsync();
        DataCollectSuccess(url, requestGuid, responseStr, dataCollectionReason);

        var deserialized = JsonConvert.DeserializeObject<ClusterCallerUserInfo>(responseStr)!;
        return new ImmutableNonIndexedAccount
        {
            namedApp = NamedApp.Cluster,
            qualifiedAppName = ClusterAuthority.QualifiedAppName,
            inAppIdentifier = deserialized.userId,
            inAppDisplayName = deserialized.displayName,
            specifics = new ImmutableClusterSpecifics
            {
                bio = deserialized.bio,
                username = deserialized.username
            },
            callers = [new ImmutableCallerAccount
            {
                isAnonymous = false,
                inAppIdentifier = deserialized.userId,
                isContact = true,
                note = new ImmutableNote
                {
                    status = NoteState.NeverHad,
                    text = null
                }
            }]
        };
    }

    public async Task<List<ClusterUserInfo>> GetFriends(DataCollectionReason dataCollectionReason, int pageSize = 30)
    {
        string UrlBuilder(int page) => $"{AuditUrls.ClusterApiUrlV1}/user_friends_all?page_Sze={pageSize}";

        var url = UrlBuilder(pageSize);
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Bearer", _bearer__sensitive);
        var requestGuid = XYVRGuids.ForRequest();

        // TODO: Pagination using "next" contained in the response
        var response = await _client.SendAsync(request, _cancellationTokenSource.Token);
        EnsureSuccessOrThrowVerbose(response);

        var responseStr = await response.Content.ReadAsStringAsync();
        DataCollectSuccess(url, requestGuid, responseStr, dataCollectionReason);

        var deserialized = JsonConvert.DeserializeObject<ClusterPaginatedUsersResponse>(responseStr)!;
        return deserialized.users.Select(state => state.user).ToList();
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

    public void Provide(ClusterAuthStorage authStorage__sensitive)
    {
        _bearer__sensitive = authStorage__sensitive.bearer;
    }
}