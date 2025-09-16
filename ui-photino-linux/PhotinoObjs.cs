namespace XYVR.UI.Photino;

#pragma warning disable 0649
[Serializable]
internal class PhotinoSendMessage
{
    public required string id;
    public required PhotinoSendMessagePayload payload;
}

[Serializable]
internal class PhotinoSendMessagePayload
{
    public required string endpoint;
    public required string methodName;
    public required object[] parameters = [];
}

[Serializable]
internal class PhotinoReceiveMessage
{
    public required bool isPhotinoMessage;
    public required bool isEvent;
    
    public required string id;
    public string? payload;
    public required bool isError;
}
#pragma warning restore 0649