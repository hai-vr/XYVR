using System.Net;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using XYVR.API.Audit;
using XYVR.Core;

namespace XYVR.AccountAuthority.Resonite;

// Based on https://wiki.resonite.com/API
internal class ResoniteAPI
{
    private const string ResoniteApiSourceName = "resonite_web_api";

    private readonly string _secretMachineId;
    private readonly string _uid;
    private readonly IResponseCollector _responseCollector;
    private readonly bool _useRateLimiting;

    private readonly CookieContainer _cookies;
    private readonly HttpClient _client;
    private readonly Random _random = new();

    private string _myUserId;
    private string? _token__sensitive;

    public ResoniteAPI(string secretMachineId_isGuid, string uid_isSha256Hash, IResponseCollector responseCollector, bool useRateLimiting = true)
    {
        _secretMachineId = secretMachineId_isGuid;
        _uid = uid_isSha256Hash;
        _responseCollector = responseCollector;
        _useRateLimiting = useRateLimiting;

        _cookies = new CookieContainer();
        var handler = new HttpClientHandler
        {
            CookieContainer = _cookies
        };
        _client = new HttpClient(handler);
        _client.DefaultRequestHeaders.UserAgent.ParseAdd(XYVRValues.UserAgent);
    }

    public async Task<LoginResponseJsonObject> Login(string username__sensitive, string password__sensitive, bool stayLoggedIn, string? twoferTotp = null)
    {
        var obj__sensitive = new LoginJsonObject
        {
            username = username__sensitive,
            authentication = new AuthenticationJsonObject
            {
                type = "password",
                password = password__sensitive
            },
            secretMachineId = _secretMachineId,
            rememberMe = stayLoggedIn
        };
        var request__sensitive = new HttpRequestMessage(HttpMethod.Post, $"{AuditUrls.ResoniteApiUrl}/userSessions");
        request__sensitive.Content = ToCarefulJsonContent__Sensitive(obj__sensitive);
        request__sensitive.Headers.Add("UID", _uid);
        if (twoferTotp != null) request__sensitive.Headers.Add("TOTP", twoferTotp);

        var response__sensitive = await _client.SendAsync(request__sensitive);
        
        EnsureSuccessOrThrowVerbose(response__sensitive);

        var response = JsonConvert.DeserializeObject<LoginResponseJsonObject>(await response__sensitive.Content.ReadAsStringAsync());

        _myUserId = response.entity.userId;
        _token__sensitive = response.entity.token;

        return response;
    }

    private static StringContent ToCarefulJsonContent__Sensitive(LoginJsonObject obj__sensitive)
    {
        return new StringContent(JsonConvert.SerializeObject(obj__sensitive), Encoding.UTF8, "application/json");
    }

    private static StringContent ToJsonContent(object obj)
    {
        return new StringContent(JsonConvert.SerializeObject(obj), Encoding.UTF8, "application/json");
    }

    public static string RandomUID__NotCryptographicallySecure()
    {
        var randomBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        
        using (var sha256 = SHA256.Create())
        {
            var hashBytes = sha256.ComputeHash(randomBytes);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
    }
    
    private static void EnsureSuccessOrThrowVerbose(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Request failed with status {response.StatusCode}, reason: {response.ReasonPhrase}");
        }
    }
    
    public async Task<ContactResponseElementJsonObject[]> GetUserContacts(DataCollectionReason dataCollectionReason)
    {
        var url = $"{AuditUrls.ResoniteApiUrl}/users/{_myUserId}/contacts";
        var requestGuid = XYVRGuids.ForRequest();
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"res {_myUserId}:{_token__sensitive}");

            var response = await _client.SendAsync(request);
            await EnsureRateLimiting(url);

            EnsureSuccessOrThrowVerbose(response);

            var responseStr = await response.Content.ReadAsStringAsync();
            DataCollectSuccess(url, requestGuid, responseStr, dataCollectionReason);
            return JsonConvert.DeserializeObject<ContactResponseElementJsonObject[]>(responseStr);
        }
        catch (Exception _)
        {
            DataCollectFailure(url, requestGuid, dataCollectionReason);
            throw;
        }
    }

    public async Task<UserResponseJsonObject> GetUser__self(DataCollectionReason dataCollectionReason)
    {
        return (UserResponseJsonObject)(await GetUser(_myUserId, dataCollectionReason))!;
    }

    public async Task<UserResponseJsonObject?> GetUser(string userId, DataCollectionReason dataCollectionReason)
    {
        var url = $"{AuditUrls.ResoniteApiUrl}/users/{userId}";
        var requestGuid = XYVRGuids.ForRequest();
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"res {_myUserId}:{_token__sensitive}");

            var response = await _client.SendAsync(request);
            await EnsureRateLimiting(url);
            
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                DataCollectNotFound(url, requestGuid, await response.Content.ReadAsStringAsync(), dataCollectionReason);
                return null;
            }

            EnsureSuccessOrThrowVerbose(response);

            var responseStr = await response.Content.ReadAsStringAsync();
            DataCollectSuccess(url, requestGuid, responseStr, dataCollectionReason);
            return JsonConvert.DeserializeObject<UserResponseJsonObject>(responseStr);
        }
        catch (Exception _)
        {
            DataCollectFailure(url, requestGuid, dataCollectionReason);
            throw;
        }
    }

    public async Task<string> GetSessions__Temp(DataCollectionReason dataCollectionReason)
    {
        var url = $"{AuditUrls.ResoniteApiUrl}/sessions";
        var requestGuid = XYVRGuids.ForRequest();
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"res {_myUserId}:{_token__sensitive}");

            var response = await _client.SendAsync(request);
            await EnsureRateLimiting(url);
            
            EnsureSuccessOrThrowVerbose(response);

            var responseStr = await response.Content.ReadAsStringAsync();
            DataCollectSuccess(url, requestGuid, responseStr, dataCollectionReason);
            return responseStr;
            // return JsonConvert.DeserializeObject<UserResponseJsonObject>(responseStr);
        }
        catch (Exception _)
        {
            DataCollectFailure(url, requestGuid, dataCollectionReason);
            throw;
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
            apiSource = ResoniteApiSourceName,
            route = url,
            status = DataCollectionResponseStatus.Success,
            responseObject = responseStr,
            metaObject = null,
        });
    }
    
    private void DataCollectNotFound(string url, string requestGuid, string responseStr, DataCollectionReason dataCollectionReason)
    {
        _responseCollector.Ingest(new ResponseCollectionTrail
        {
            timestamp = _responseCollector.GetCurrentTime(),
            trailGuid = XYVRGuids.ForTrail(),
            requestGuid = requestGuid,
            reason = dataCollectionReason,
            apiSource = ResoniteApiSourceName,
            route = url,
            status = DataCollectionResponseStatus.NotFound,
            responseObject = responseStr,
            metaObject = null,
        });
    }

    private void DataCollectFailure(string url, string requestGuid, DataCollectionReason dataCollectionReason)
    {
        _responseCollector.Ingest(new ResponseCollectionTrail
        {
            timestamp = _responseCollector.GetCurrentTime(),
            trailGuid = XYVRGuids.ForTrail(),
            requestGuid = requestGuid,
            reason = dataCollectionReason,
            apiSource = ResoniteApiSourceName,
            route = url,
            status = DataCollectionResponseStatus.Failure,
            responseObject = null,
            metaObject = null,
        });
    }

    public string GetAllUserAndToken__Sensitive()
    {
        return JsonConvert.SerializeObject(new ResoniteAuthStorage
        {
            userId = _myUserId,
            token = _token__sensitive
        });
    }

    public void ProvideUserAndToken(string userAndToken__sensitive)
    {
        var resAuthenticationStorage__sensitive = JsonConvert.DeserializeObject<ResoniteAuthStorage>(userAndToken__sensitive);
        _myUserId = resAuthenticationStorage__sensitive.userId;
        _token__sensitive = resAuthenticationStorage__sensitive.token;
    }

    private async Task EnsureRateLimiting(string urlForLogging)
    {
        if (!_useRateLimiting) return;

        var millisecondsDelay = _random.Next(400, 600); // Introduce some irregularity
        Console.WriteLine($"Got {urlForLogging} ; Waiting {millisecondsDelay}ms...");

        await Task.Delay(millisecondsDelay);
    }
}