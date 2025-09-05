namespace XYVR.API.Resonite;

public class UserStatusUpdate
{
    public string userId;
    public string userSessionId;
    public string sessionType;
    public string? outputDevice;
    public bool isMobile;
    public string onlineStatus;
    public bool isPresent;
    public DateTime? lastPresenceTimestamp;
    public DateTime lastStatusChange;
    public string? hashSalt;
    public string appVersion;
    public string? compatibilityHash;
    // public OBJECT publicRSAKey;
    public List<Session> sessions = new();
    public int currentSessionIndex;
}

public class Session
{
    public string sessionHash;
    public string accessLevel;
    public bool sessionHidden;
    public bool isHost;
    public string? broadcastKey;
}