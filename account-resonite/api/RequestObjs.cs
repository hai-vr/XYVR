using Newtonsoft.Json;

namespace XYVR.API.Resonite;

#pragma warning disable CS8618
#pragma warning disable 0649
internal class LoginJsonObject
{
    public string username;
    public AuthenticationJsonObject authentication;
    public string secretMachineId;
    public bool rememberMe;
}

internal class AuthenticationJsonObject
{
    [JsonProperty("$type")]
    public string type;
    public string password;
}
#pragma warning restore 0649
#pragma warning restore CS8618
