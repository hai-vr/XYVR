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
