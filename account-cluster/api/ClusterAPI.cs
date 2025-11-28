using Newtonsoft.Json;
using XYVR.API.Audit;
using XYVR.Core;

namespace XYVR.AccountAuthority.Cluster;

internal class ClusterAPI
{
    private const string ApiSourceName = "cluster_web_api";
    
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
        AddAuthHeader(request);
        var requestGuid = XYVRGuids.ForRequest();

        var response = await _client.SendAsync(request, _cancellationTokenSource.Token);
        await EnsureSuccessOrThrowVerbose(response);

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

    public async Task<List<ClusterUserState>> GetFriends(DataCollectionReason dataCollectionReason, int pageSize = 30)
    {
        string UrlBuilder(int page, string? nextToken)
        {
            if (nextToken == null)
            {
                return $"{AuditUrls.ClusterApiUrlV1}/user_friends_all?pageSize={pageSize}";
            }
            
            return $"{AuditUrls.ClusterApiUrlV1}/user_friends_all?pageSize={pageSize}&pageToken={nextToken}";
        }
        var allResults = new List<ClusterUserState>();
        
        string? nextToken = null;
        try
        {
            ClusterPaginatedUsersResponse deserialized;
            do
            {
                var url = UrlBuilder(pageSize, nextToken);
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                AddAuthHeader(request);
                var requestGuid = XYVRGuids.ForRequest();

                var response = await _client.SendAsync(request, _cancellationTokenSource.Token);
                await EnsureSuccessOrThrowVerbose(response);

                var responseStr = await response.Content.ReadAsStringAsync();
                XYVRLogging.WriteLine(this, $"Got {url}");
                DataCollectSuccess(url, requestGuid, responseStr, dataCollectionReason);
            
                deserialized = JsonConvert.DeserializeObject<ClusterPaginatedUsersResponse>(responseStr)!;
                allResults.AddRange(deserialized.users.ToList());

                if (deserialized.paging.nextToken != "")
                {
                    nextToken = deserialized.paging.nextToken;
                    await Task.Delay(200);
                }

            } while (deserialized.paging.nextToken != "");
        }
        catch (Exception e)
        {
            XYVRLogging.ErrorWriteLine(this, e);
            throw;
        }
        
        return allResults;
    }

    public async Task<ClusterHotsResponse> GetHots(DataCollectionReason dataCollectionReason, int pageSize = 100)
    {
        string UrlBuilder(int page) => $"{AuditUrls.ClusterApiUrlV1}/live_activity/friend_hots?page_Sze={pageSize}";

        var url = UrlBuilder(pageSize);
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        AddAuthHeader(request);
        var requestGuid = XYVRGuids.ForRequest();

        var response = await _client.SendAsync(request, _cancellationTokenSource.Token);
        await EnsureSuccessOrThrowVerbose(response);

        var responseStr = await response.Content.ReadAsStringAsync();
        DataCollectSuccess(url, requestGuid, responseStr, dataCollectionReason);

        return JsonConvert.DeserializeObject<ClusterHotsResponse>(responseStr)!;
    }

    private static async Task EnsureSuccessOrThrowVerbose(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Request failed with status {response.StatusCode}, reason: {response.ReasonPhrase}, body: {errorMessage}");
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
            apiSource = ApiSourceName,
            route = url,
            status = DataCollectionResponseStatus.Success,
            responseObject = responseStr,
            metaObject = null,
        });
    }

    private void AddAuthHeader(HttpRequestMessage request)
    {
        if (_bearer__sensitive.StartsWith("Bearer ", StringComparison.InvariantCultureIgnoreCase))
        {
            request.Headers.Add("Authorization", $"{_bearer__sensitive}");
        }
        else
        {
            request.Headers.Add("Authorization", $"Bearer {_bearer__sensitive}");
        }
    }

    public void Provide(ClusterAuthStorage authStorage__sensitive)
    {
        _bearer__sensitive = authStorage__sensitive.bearer;
    }
}