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

    public async Task<HttpResponseMessage> CreateToken(string username__sensitive, string password__sensitive, string? twoferTotp = null)
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
        var request = new HttpRequestMessage(HttpMethod.Post, $"{PrefixWithSlash}userSessions");
        request.Content = ToCarefulJsonContent__Sensitive(obj__sensitive);
        request.Headers.Add("UID", _uid);
        if (twoferTotp != null) request.Headers.Add("TOTP", twoferTotp);

        var response = await _client.SendAsync(request);
        return response;
    }

    private static StringContent ToCarefulJsonContent__Sensitive(LoginJsonObject obj__sensitive)
    {
        return new StringContent(JsonConvert.SerializeObject(obj__sensitive), Encoding.UTF8, "application/json");
    }

    private static StringContent ToJsonContent(LoginJsonObject obj)
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
}

public struct LoginJsonObject
{
    public string username;
    public AuthenticationJsonObject authentication;
    public string secretMachineId;
}

public class AuthenticationJsonObject
{
    [JsonProperty("$type")]
    public string type;
    public string password;
}