using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using XYVR.Core;
using XYVR.Data.Collection;

namespace XYVR.API.VRChat;

public class VRChatAPI
{
    private const string VRChatApiSourceName = "vrchat_web_api";
    
    // https://github.com/vrchatapi/specification/commit/558c0ca50202c45194a49d515f27e64f62079ba4#diff-5fa520d3bb34f9ae444cdbdf2b9eccff2361eb89a0cd3f4dba1e2e0fa9bba452R15
    // https://discord.com/channels/418093857394262020/418512124529344523/1303873667473866752
    // "Yes, going forward, all API requests need to go through api.vrchat.cloud instead"
    private const string RootUrl = "https://api.vrchat.cloud/api/1"; // Formerly: "https://vrchat.com/api/1"
    private const string CookieDomainBit = "api.vrchat.cloud";
    private const string CookieDomain = $"https://{CookieDomainBit}";
    private const string AuthUrl = RootUrl + "/auth/user";
    private const string LogoutUrl = RootUrl + "/logout";
    private const string EmailOtpUrl = RootUrl + "/auth/twofactorauth/emailotp/verify";
    private const string OtpUrl = RootUrl + "/auth/twofactorauth/otp/verify";

    private readonly IResponseCollector _responseCollector;
    
    private readonly bool _useRateLimiting;
    private readonly Random _random = new();
    
    private CookieContainer _cookies;
    private HttpClient _client;
    private string? _userAgent;

    public bool IsLoggedIn { get; private set; }

    public VRChatAPI(IResponseCollector responseCollector, bool useRateLimiting = true)
    {
        _responseCollector = responseCollector;
        _useRateLimiting = useRateLimiting;
        
        _cookies = new CookieContainer();
        var handler = new HttpClientHandler
        {
            CookieContainer = _cookies
        };
        _client = new HttpClient(handler);
        _userAgent = $"Hai.XYVR/{VERSION.version} (docs.hai-vr.dev/docs/products/xyvr#user-agent)";
        _client.DefaultRequestHeaders.UserAgent.ParseAdd(_userAgent);
    }

    public string GetAllCookies__Sensitive()
    {
        return JsonConvert.SerializeObject(CompileCookies());
    }

    public void ProvideCookies(string userinput_cookies__sensitive)
    {
        _cookies = new CookieContainer();
        var deserialized = JsonConvert.DeserializeObject<VrcAuthenticationCookies>(userinput_cookies__sensitive);
        if (deserialized.auth != null) _cookies.Add(new Uri(CookieDomain), RebuildCookie(deserialized.auth, "auth"));
        if (deserialized.twoFactorAuth != null) _cookies.Add(new Uri(CookieDomain), RebuildCookie(deserialized.twoFactorAuth, "twoFactorAuth"));
        var handler = new HttpClientHandler
        {
            CookieContainer = _cookies
        };
        _client = new HttpClient(handler);
        _client.DefaultRequestHeaders.UserAgent.ParseAdd(_userAgent);
        
        // Assume that if the user has an auth cookie, then they're logged in.
        // There is a route to check if the token is still valid, but for privacy, we don't want the application to send a request
        // to VRChat's server every time we start.
        // VRChat should only be privileged to know when a user is actively using this app.
        IsLoggedIn = deserialized.auth != null && deserialized.twoFactorAuth != null;
    }

    private static Cookie RebuildCookie(VrcCookie cookie, string name)
    {
        return new Cookie
        {
            Domain = CookieDomainBit,
            Name = name,
            Value = cookie.Value,
            Expires = cookie.Expires,
            HttpOnly = true,
            Path = "/"
        };
    }

    private VrcAuthenticationCookies CompileCookies()
    {
        var subCookies = _cookies.GetCookies(new Uri(CookieDomain)).ToArray();
        var authNullable = subCookies.Where(cookie => cookie.Name == "auth").Select(Cookify).FirstOrDefault();
        var twoFactorAuthNullable = subCookies.Where(cookie => cookie.Name == "twoFactorAuth").Select(Cookify).FirstOrDefault();
        
        return new VrcAuthenticationCookies
        {
            auth = authNullable,
            twoFactorAuth = twoFactorAuthNullable
        };
    }

    private VrcCookie Cookify(Cookie cookie)
    {
        return new VrcCookie
        {
            Value = cookie.Value,
            Expires = cookie.Expires,
        };
    }

    [Serializable]
    public class VrcAuthenticationCookies
    {
        public VrcCookie auth;
        public VrcCookie twoFactorAuth;
    }

    [Serializable]
    public class VrcCookie
    {
        public string Value;
        public DateTime Expires;
    }
    
    
    public async Task<LoginResponse> Login(string userinput_account__sensitive, string userinput_password__sensitive)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, AuthUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", EncodeBasicAuth__Sensitive(userinput_account__sensitive, userinput_password__sensitive));
        
        var response = await _client.SendAsync(request);
        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        return response.StatusCode switch
        {
            HttpStatusCode.OK => await ParseLoginOk(response),
            HttpStatusCode.Unauthorized => new LoginResponse { Status = LoginResponseStatus.Failure },
            _ => new LoginResponse { Status = LoginResponseStatus.OutsideProtocol }
        };
    }

    private async Task<LoginResponse> ParseLoginOk(HttpResponseMessage response)
    {
        // The response will Set-Cookie onto our client, if we don't have it already.
        var content = await response.Content.ReadAsStringAsync();
        var hasTwofer = JObject.Parse(content).TryGetValue("requiresTwoFactorAuth", out JToken twoferMethod);
        if (hasTwofer)
        {
            return new LoginResponse
            {
                Status = LoginResponseStatus.RequiresTwofer,
                TwoferMethod = twoferMethod.Values<string>().Contains("emailOtp") ? TwoferMethod.Email : TwoferMethod.Other
            };
        }

        // This happens if our request also had the twofer cookie
        IsLoggedIn = true;
        return new LoginResponse
        {
            Status = LoginResponseStatus.Success
        };
    }

    public async Task<LoginResponse> VerifyTwofer(string userinput_twoferCode__sensitive, TwoferMethod method)
    {
        // TODO: Sanitize the user input
        
        // Our client has the auth cookie that was set as a result of a successful auth that lead to a twofer.
        var request = new HttpRequestMessage(HttpMethod.Post, method == TwoferMethod.Email ? EmailOtpUrl : OtpUrl);
        request.Content = new StringContent(JObject.FromObject(new TwoferRequestPayload
        {
            code = userinput_twoferCode__sensitive
        }).ToString(), Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));
        
        var response = await _client.SendAsync(request);

        return response.StatusCode switch
        {
            HttpStatusCode.OK => await ParseVerifyOk(response),
            HttpStatusCode.Unauthorized => new LoginResponse { Status = LoginResponseStatus.Failure },
            _ => new LoginResponse { Status = LoginResponseStatus.OutsideProtocol }
        };
    }

    private async Task<LoginResponse> ParseVerifyOk(HttpResponseMessage response)
    {
        // The response will Set-Cookie the twofer when successful.
        var content = await response.Content.ReadAsStringAsync();
        var hasVerified = JObject.Parse(content).TryGetValue("verified", out JToken verifyResult);
        if (hasVerified)
        {
            IsLoggedIn = true;
            return new LoginResponse
            {
                Status = verifyResult.Value<bool>() ? LoginResponseStatus.Success : LoginResponseStatus.Failure
            };
        }

        return new LoginResponse
        {
            Status = LoginResponseStatus.OutsideProtocol
        };
    }

    private string EncodeBasicAuth__Sensitive(string userinput_account__sensitive, string userinput_password__sensitive)
    {
        var basicToken__sensitive = $"{HttpUtility.UrlEncode(userinput_account__sensitive)}:{HttpUtility.UrlEncode(userinput_password__sensitive)}";
        var bytes__sensitive = Encoding.UTF8.GetBytes(basicToken__sensitive);
        var result__sensitive = Convert.ToBase64String(bytes__sensitive);
        return result__sensitive;
    }

    public async Task<VRChatAuthUser> GetAuthUser(DataCollectionReason dataCollectionReason)
    {
        ThrowIfNotLoggedIn();
        
        var url = $"{RootUrl}/auth/user";
        var requestGuid = Guid.NewGuid().ToString();

        try
        {
            var response = await _client.GetAsync(url);

            await EnsureRateLimiting(url);

            EnsureSuccessOrThrowVerbose(response);

            var responseStr = await response.Content.ReadAsStringAsync();

            DataCollectSuccess(url, requestGuid, responseStr, dataCollectionReason);

            return JsonConvert.DeserializeObject<VRChatAuthUser>(responseStr);
        }
        catch (Exception _)
        {
            DataCollectFailure(url, requestGuid, dataCollectionReason);
            throw;
        }
    }

    public async Task<List<VRChatFriend>> ListFriends(ListFriendsRequestType listFriendsRequestType, DataCollectionReason dataCollectionReason)
    {
        ThrowIfNotLoggedIn();
        
        var requestGuid = Guid.NewGuid().ToString();

        var offline = listFriendsRequestType == ListFriendsRequestType.OnlyOffline ? "true" : "false";
        return await GetPaginatedResults<VRChatFriend>(dataCollectionReason, requestGuid, (offset, pageSize) =>
            Task.FromResult($"{RootUrl}/auth/user/friends?offset={offset}&n={pageSize}&offline={offline}"));
    }

    public async Task<VRChatUser?> GetUserLenient(string userId, DataCollectionReason dataCollectionReason)
    {
        ThrowIfNotLoggedIn();
        
        var url = $"{RootUrl}/users/{userId}";
        var requestGuid = Guid.NewGuid().ToString();
        try
        {
            var response = await _client.GetAsync(url);

            await EnsureRateLimiting(url);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                DataCollectNotFound(url, requestGuid, await response.Content.ReadAsStringAsync(), dataCollectionReason);
                return null;
            }

            EnsureSuccessOrThrowVerbose(response);

            var responseStr = await response.Content.ReadAsStringAsync();
            DataCollectSuccess(url, requestGuid, responseStr, dataCollectionReason);
            return JsonConvert.DeserializeObject<VRChatUser>(responseStr);
        }
        catch (Exception _)
        {
            DataCollectFailure(url, requestGuid, dataCollectionReason);
            throw;
        }
    }

    public async Task<List<VRChatNoteFull>> ListUserNotes(DataCollectionReason dataCollectionReason)
    {
        ThrowIfNotLoggedIn();
        
        var requestGuid = Guid.NewGuid().ToString();
        
        return await GetPaginatedResults<VRChatNoteFull>(dataCollectionReason, requestGuid, (offset, pageSize) =>
            Task.FromResult($"{RootUrl}/userNotes?offset={offset}&n={pageSize}"));
    }
    
    private async Task<List<T>> GetPaginatedResults<T>(DataCollectionReason dataCollectionReason, string requestGuid, Func<int, int, Task<string>> urlBuilder, int pageSize = 100)
    {
        string? url = null;
        try
        {
            var allResults = new List<T>();
            var hasMoreData = true;
            var offset = 0;

            while (hasMoreData)
            {
                url = await urlBuilder(offset, pageSize);
                var response = await _client.GetAsync(url);
        
                await EnsureRateLimiting(url);
        
                EnsureSuccessOrThrowVerbose(response);

                var responseStr = await response.Content.ReadAsStringAsync();
                DataCollectSuccess(url, requestGuid, responseStr, dataCollectionReason);
                var results = JsonConvert.DeserializeObject<List<T>>(responseStr);
                if (results == null || results.Count == 0)
                {
                    hasMoreData = false;
                }
                else
                {
                    allResults.AddRange(results);

                    if (results.Count < pageSize)
                    {
                        hasMoreData = false;
                    }
                    else
                    {
                        offset += pageSize;
                    }
                }
            }

            return allResults;
        }
        catch (Exception _)
        {
            if (url != null) DataCollectFailure(url, requestGuid, dataCollectionReason);
            throw;
        }
    }

    private async Task EnsureRateLimiting(string urlForLogging)
    {
        if (!_useRateLimiting) return;

        var millisecondsDelay = _random.Next(700, 1300); // Introduce some irregularity
        Console.WriteLine($"Got {urlForLogging} ; Waiting {millisecondsDelay}ms...");

        await Task.Delay(millisecondsDelay);
    }

    private static void EnsureSuccessOrThrowVerbose(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Request failed with status {response.StatusCode}, reason: {response.ReasonPhrase}");
        }
    }

    private void ThrowIfNotLoggedIn()
    {
        if (!IsLoggedIn) throw new HttpRequestException("Application does not have the cookie to be logged in.");
    }
    
    private void DataCollectSuccess(string url, string requestGuid, string responseStr, DataCollectionReason dataCollectionReason)
    {
        _responseCollector.Ingest(new ResponseCollectionTrail
        {
            timestamp = _responseCollector.GetCurrentTime(),
            trailGuid = Guid.NewGuid().ToString(),
            requestGuid = requestGuid,
            reason = dataCollectionReason,
            apiSource = VRChatApiSourceName,
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
            trailGuid = Guid.NewGuid().ToString(),
            requestGuid = requestGuid,
            reason = dataCollectionReason,
            apiSource = VRChatApiSourceName,
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
            trailGuid = Guid.NewGuid().ToString(),
            requestGuid = requestGuid,
            reason = dataCollectionReason,
            apiSource = VRChatApiSourceName,
            route = url,
            status = DataCollectionResponseStatus.Failure,
            responseObject = null,
            metaObject = null,
        });
    }

    public async Task<LogoutResponseStatus> Logout()
    {
        if (!IsLoggedIn) return LogoutResponseStatus.NotLoggedIn;
        IsLoggedIn = false;
    
        var request = new HttpRequestMessage(HttpMethod.Put, LogoutUrl);
    
        var response = await _client.SendAsync(request);
        return response.StatusCode switch
        {
            HttpStatusCode.OK => LogoutResponseStatus.Success,
            HttpStatusCode.Unauthorized => LogoutResponseStatus.Unauthorized,
            _ => LogoutResponseStatus.OutsideProtocol
        };
    }
}

public enum ListFriendsRequestType
{
    OnlyOnline,
    OnlyOffline,
}

public struct LoginResponse
{
    public LoginResponseStatus Status;
    public TwoferMethod TwoferMethod;
}

public enum LoginResponseStatus
{
    Unresolved, OutsideProtocol, Failure, Success, RequiresTwofer
}

public enum LogoutResponseStatus
{
    Unresolved, OutsideProtocol, Success, Unauthorized, NotLoggedIn
}

public enum TwoferMethod
{
    Other, Email
}

[Serializable]
public class TwoferRequestPayload
{
    // ReSharper disable once InconsistentNaming
    public string code;
}