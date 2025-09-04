using XYVR.Core;

namespace XYVR.Data.Collection;

public class ConnectionAttempt
{
    public Connector connector;
    public string? login__sensitive;
    public string? password__sensitive;
    public string? twoFactorCode__sensitive;
    public bool stayLoggedIn;
    public bool isTwoFactorEmail;
}

public class ConnectionAttemptResult
{
    public string guid;
    public ConnectionAttemptResultType type;
    public ConnectorAccount account;
    public bool isTwoFactorEmail; // This is not unused, it's read by the front.
}

public enum ConnectionAttemptResultType
{
    Failure,
    Success,
    NeedsTwoFactorCode,
    LoggedOut
}