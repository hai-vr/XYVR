using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using XYVR.API.Audit;
using XYVR.Core;
using XYVR.Data.Collection;

namespace XYVR.API.VRChat;

public class VRChatAPI
{
    private const string VRChatApiSourceName = "vrchat_web_api";
    private const string AuthUrl = AuditUrls.VrcApiUrl + "/auth/user";
    private const string LogoutUrl = AuditUrls.VrcApiUrl + "/logout";
    private const string EmailOtpUrl = AuditUrls.VrcApiUrl + "/auth/twofactorauth/emailotp/verify";
    private const string TotpUrl = AuditUrls.VrcApiUrl + "/auth/twofactorauth/totp/verify";

    private readonly IResponseCollector _responseCollector;
    
    private readonly bool _useRateLimiting;
    private readonly Random _random = new();
    
    private CookieContainer _cookies;
    private HttpClient _client;
    private readonly string _userAgent;

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
        _userAgent = $"Hai.XYVR/{VERSION.version} (docs.hai-vr.dev/docs/products/xyvr/user-agent)";
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
        if (deserialized.auth != null) _cookies.Add(new Uri(AuditUrls.VrcCookieDomain), RebuildCookie(deserialized.auth, "auth"));
        if (deserialized.twoFactorAuth != null) _cookies.Add(new Uri(AuditUrls.VrcCookieDomain), RebuildCookie(deserialized.twoFactorAuth, "twoFactorAuth"));
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
            Domain = AuditUrls.VrcCookieDomainBit,
            Name = name,
            Value = cookie.Value,
            Expires = cookie.Expires,
            HttpOnly = true,
            Path = "/"
        };
    }

    private VrcAuthenticationCookies CompileCookies()
    {
        var subCookies = _cookies.GetCookies(new Uri(AuditUrls.VrcCookieDomain)).ToArray();
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
        var request = new HttpRequestMessage(HttpMethod.Post, method == TwoferMethod.Email ? EmailOtpUrl : TotpUrl);
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
        
        var url = $"{AuditUrls.VrcApiUrl}/auth/user";
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

    public async IAsyncEnumerable<VRChatFriend> ListFriends(ListFriendsRequestType listFriendsRequestType, DataCollectionReason dataCollectionReason)
    {
        ThrowIfNotLoggedIn();
        
        var requestGuid = Guid.NewGuid().ToString();

        var offline = listFriendsRequestType == ListFriendsRequestType.OnlyOffline ? "true" : "false";
        await foreach (var friend in GetPaginatedResults<VRChatFriend>(dataCollectionReason, requestGuid, (offset, pageSize) =>
                           Task.FromResult($"{AuditUrls.VrcApiUrl}/auth/user/friends?offset={offset}&n={pageSize}&offline={offline}")))
        {
            yield return friend;
        }
    }


    public async Task<VRChatUser?> GetUserLenient(string userId, DataCollectionReason dataCollectionReason)
    {
        ThrowIfNotLoggedIn();
        
        var url = $"{AuditUrls.VrcApiUrl}/users/{userId}";
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

    public async IAsyncEnumerable<VRChatNoteFull> ListUserNotes(DataCollectionReason dataCollectionReason)
    {
        ThrowIfNotLoggedIn();
        
        var requestGuid = Guid.NewGuid().ToString();
        
        await foreach (var note in GetPaginatedResults<VRChatNoteFull>(dataCollectionReason, requestGuid, (offset, pageSize) =>
            Task.FromResult($"{AuditUrls.VrcApiUrl}/userNotes?offset={offset}&n={pageSize}")))
        {
            yield return note;
        }
    }
    
    private async IAsyncEnumerable<T> GetPaginatedResults<T>(DataCollectionReason dataCollectionReason, string requestGuid, Func<int, int, Task<string>> urlBuilder, int pageSize = 100)
    {
        var hasMoreData = true;
        var offset = 0;

        while (hasMoreData)
        {
            List<T>? results;
            string? url = null;
            
            try
            {
                url = await urlBuilder(offset, pageSize);
                var response = await _client.GetAsync(url);
        
                await EnsureRateLimiting(url);
        
                EnsureSuccessOrThrowVerbose(response);

                var responseStr = await response.Content.ReadAsStringAsync();
                DataCollectSuccess(url, requestGuid, responseStr, dataCollectionReason);
                results = JsonConvert.DeserializeObject<List<T>>(responseStr);
            }
            catch (Exception _)
            {
                if (url != null) DataCollectFailure(url, requestGuid, dataCollectionReason);
                throw;
            }
            
            if (results == null || results.Count == 0)
            {
                hasMoreData = false;
            }
            else
            {
                foreach (var result in results)
                {
                    yield return result;
                }

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