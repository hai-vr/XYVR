namespace XYVR.AccountAuthority.ChilloutVR;

#pragma warning disable CS8618
#pragma warning disable 0649
[Serializable]
internal record CvrAuthenticationRequest
{
    public required CvrAuthType AuthType { get; init; }
    public required string Username { get; init; }
    public required string Password { get; init; }
}

[Serializable]
internal class CvrLoginResponse
{
    public CvrLoginResponseStatus Status;
    public CvrAuth? Auth;
}

[Serializable]
internal class CvrAuth
{
    public required string message;
    public required CvrAuthData data;
}

[Serializable]
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

internal enum CvrLoginResponseStatus
{
    Unresolved,
    OutsideProtocol,
    Failure,
    Success,
    RequiresTwofer
}

internal enum CvrAuthType
{
    AccessKey = 1,
    Password = 2
}
#pragma warning restore 0649
#pragma warning restore CS8618