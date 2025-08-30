using System.Net;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace XYVR.API.Resonite;

// Based on https://wiki.resonite.com/API
public class ResoniteAPI
{
    private static readonly string PrefixWithSlash = "https://api.resonite.com/";
    
    private readonly CookieContainer _cookies;
    private readonly HttpClient _client;
    
    private readonly string _secretMachineId;
    private readonly string _uid;
    
    private string _authHeader__sensitive;
    private string _myUserId;

    public ResoniteAPI(string secretMachineId_isGuid, string uid_isSha256Hash)
    {
        _secretMachineId = secretMachineId_isGuid;
        _uid = uid_isSha256Hash;
        
        _cookies = new CookieContainer();
        var handler = new HttpClientHandler
        {
            CookieContainer = _cookies
        };
        _client = new HttpClient(handler);
        _client.DefaultRequestHeaders.UserAgent.ParseAdd($"Hai.XYVR/{VERSION.version} (docs.hai-vr.dev/docs/products/xyvr#user-agent)");
    }

    public async Task<LoginResponseJsonObject> Login(string username__sensitive, string password__sensitive, string? twoferTotp = null)
    {
        var obj__sensitive = new LoginJsonObject
        {
            username = username__sensitive,
            authentication = new AuthenticationJsonObject
            {
                type = "password",
                password = password__sensitive
            },
            secretMachineId = _secretMachineId
        };
        var request__sensitive = new HttpRequestMessage(HttpMethod.Post, $"{PrefixWithSlash}userSessions");
        request__sensitive.Content = ToCarefulJsonContent__Sensitive(obj__sensitive);
        request__sensitive.Headers.Add("UID", _uid);
        if (twoferTotp != null) request__sensitive.Headers.Add("TOTP", twoferTotp);

        var response__sensitive = await _client.SendAsync(request__sensitive);
        
        EnsureSuccessOrThrowVerbose(response__sensitive);

        var response = JsonConvert.DeserializeObject<LoginResponseJsonObject>(await response__sensitive.Content.ReadAsStringAsync());

        _authHeader__sensitive = ToAuthHeader(response);
        _myUserId = response.entity.userId;
        
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

    public static string ToAuthHeader(LoginResponseJsonObject response)
    {
        var userId = response.entity.userId;
        var authHeader = $"res {userId}:{response.entity.token}";
        return authHeader;
    }

    private static void EnsureSuccessOrThrowVerbose(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Request failed with status {response.StatusCode}, reason: {response.ReasonPhrase}");
        }
    }
    
    public async Task<ContactResponseElementJsonObject[]> GetUserContacts()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{PrefixWithSlash}users/{_myUserId}/contacts");
        request.Headers.Add("Authorization", _authHeader__sensitive);

        var response = await _client.SendAsync(request);
    
        EnsureSuccessOrThrowVerbose(response);
    
        var jsonContent = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<ContactResponseElementJsonObject[]>(jsonContent);
    }
    
    public async Task<UserResponseJsonObject> GetUser(string userId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{PrefixWithSlash}users/{userId}");
        request.Headers.Add("Authorization", _authHeader__sensitive);

        var response = await _client.SendAsync(request);
    
        EnsureSuccessOrThrowVerbose(response);
    
        var jsonContent = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<UserResponseJsonObject>(jsonContent);
    }
}
