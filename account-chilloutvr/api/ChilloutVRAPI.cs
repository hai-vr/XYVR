using System.Net;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using XYVR.API.Audit;
using XYVR.Core;

namespace XYVR.AccountAuthority.ChilloutVR;

internal class ChilloutVRAPI
{
    private readonly HttpClient _client;
    private string _accessKey__sensitive;
    private string _username;

    public ChilloutVRAPI()
    {
        _client = new HttpClient();
        _client.DefaultRequestHeaders.UserAgent.ParseAdd(XYVRValues.UserAgent);
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

    public async Task<CvrContactsResponse> GetContacts()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{AuditUrls.ChilloutVrApiUrlV1}/friends");
        MakeAuthenticated(request);

        var response = await _client.SendAsync(request);
        
        return response.StatusCode switch
        {
            HttpStatusCode.OK => JsonConvert.DeserializeObject<CvrContactsResponse>(await response.Content.ReadAsStringAsync()),
            _ => throw new InvalidOperationException("Outside protocol")
        };
    }

    private void MakeAuthenticated(HttpRequestMessage request)
    {
        request.Headers.Add("Username", _username);
        request.Headers.Add("AccessKey", _accessKey__sensitive);
    }

    public void Provide(ChilloutVRAuthStorage authStorage__sensitive)
    {
        _username = authStorage__sensitive.username;
        _accessKey__sensitive = authStorage__sensitive.accessKey;
    }
}