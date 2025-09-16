namespace XYVR.AccountAuthority.VRChat;

[Serializable]
internal record TwoferRequestPayload
{
    // ReSharper disable once InconsistentNaming
    public required string code { get; init; }
}

[Serializable]
internal class LoginResponse
{
    public LoginResponseStatus Status;
    public TwoferMethod TwoferMethod;
}

internal enum LoginResponseStatus
{
    Unresolved, OutsideProtocol, Failure, Success, RequiresTwofer
}

internal enum LogoutResponseStatus
{
    Unresolved, OutsideProtocol, Success, Unauthorized, NotLoggedIn
}

internal enum TwoferMethod
{
    Other, Email
}