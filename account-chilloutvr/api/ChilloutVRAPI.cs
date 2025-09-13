using System.Net;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using XYVR.API.Audit;
using XYVR.Core;

namespace XYVR.AccountAuthority.ChilloutVR;

internal class ChilloutVRAPI
{
    private readonly HttpClient _client;

    public ChilloutVRAPI()
    {
        _client = new HttpClient();
        _client.DefaultRequestHeaders.UserAgent.ParseAdd(XYVRValues.UserAgent);
    }

    public enum AuthMethod
    {
        AccessKey = 1,
        Password = 2
    }
    
    public async Task<CvrLoginResponse> Login(string userinput_account__sensitive, string userinput_password__sensitive)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{AuditUrls.ChilloutVrApiUrlV1}/users/auth");
        request.Content = new StringContent(JsonConvert.SerializeObject(new CvrAuthenticationRequest
        {
        AuthType = CvrAuthType.Password,
        Username = userinput_account__sensitive,
        Password = userinput_password__sensitive
        }), MediaTypeHeaderValue.Parse("application/json"));
        
        var response = await _client.SendAsync(request);
        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        return response.StatusCode switch
        {
            HttpStatusCode.OK => await ParseLoginOk(response),
            HttpStatusCode.Unauthorized => new CvrLoginResponse { Status = CvrLoginResponseStatus.Failure },
            _ => new CvrLoginResponse { Status = CvrLoginResponseStatus.OutsideProtocol }
        };
    }

    private async Task<CvrLoginResponse> ParseLoginOk(HttpResponseMessage response)
    {
        return new CvrLoginResponse
        {
            Status = CvrLoginResponseStatus.Success,
            Auth = JsonConvert.DeserializeObject<CvrAuth>(await response.Content.ReadAsStringAsync())!
        };
    }
}

internal record CvrAuthenticationRequest
{
    public required CvrAuthType AuthType;
    public required string Username;
    public required string Password;
}

internal class CvrLoginResponse
{
    public CvrLoginResponseStatus Status;
    public CvrAuth? Auth;
}

internal class CvrAuth
{
    public required string message;
    public required CvrAuthData data;
}

internal class CvrAuthData
{
    public string username;
    public string accessKey;
    public string userId;
    public string currentAvatar;
    public string currentHomeWorld;
    public string videoUrlResolverExecutable;
    public string videoUrlResolverHashes;
    public string[] blockedUsers;
    public string image;
}

internal class CvrAuthCredentialsStorage
{
    public string username;
    public string accessKey;
}

internal enum CvrLoginResponseStatus
{
    Unresolved, OutsideProtocol, Failure, Success, RequiresTwofer
}

internal enum CvrAuthType
{
    AccessKey = 1,
    Password = 2
}