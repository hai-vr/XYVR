using Newtonsoft.Json;

namespace XYVR.API.Resonite;

internal struct LoginJsonObject
{
    public string username;
    public AuthenticationJsonObject authentication;
    public string secretMachineId;
}

internal struct AuthenticationJsonObject
{
    [JsonProperty("$type")]
    public string type;
    public string password;
}