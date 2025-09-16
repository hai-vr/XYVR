namespace XYVR.AccountAuthority.VRChat;

[Serializable]
internal record VRChatAuthStorage
{
    public VRChatAuthCookie? auth { get; init; }
    public VRChatAuthCookie? twoFactorAuth { get; init; }
}

[Serializable]
internal record VRChatAuthCookie
{
    public required string Value { get; init; }
    public required DateTime Expires { get; init; }
}