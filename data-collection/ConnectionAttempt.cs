using XYVR.Core;

namespace XYVR.Data.Collection;

public class ConnectionAttempt
{
    public Connector connector;
    public string? login__sensitive;
    public string? password__sensitive;
    public string? twoFactorCode__sensitive;
    public bool stayLoggedIn;
}

public class ConnectionAttemptResult
{
    public string guid;
    public ConnectionAttemptResultType type;
    public ConnectorAccount account;
}

public enum ConnectionAttemptResultType
{
    Failure,
    Success,
    NeedsTwoFactorCode,
}