namespace XYVR.Core;

public class SerializedCredentials
{
    // We store the presence and absence of credentials.
    // hasAnything implies guidToPayload is not null
    // !hasAnything implies guidToPayload is null
    
    public bool hasAnything;
    public Dictionary<string, string>? guidToPayload;
}