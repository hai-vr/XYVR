using JetBrains.Annotations;
using XYVR.Core;

namespace XYVR.Login;

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
    // The React app needs this field to decide which of our endpoints to invoke, even though the backend doesn't read it.
    [PublicAPI] public bool isTwoFactorEmail;
}

public enum ConnectionAttemptResultType
{
    Failure,
    Success,
    NeedsTwoFactorCode,
    LoggedOut
}