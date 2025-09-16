namespace XYVR.AccountAuthority.ChilloutVR;

[Serializable]
internal record ChilloutVRAuthStorage
{
    public required string username { get; init; }
    public required string accessKey { get; init; }
    public required string userId { get; init; }
}