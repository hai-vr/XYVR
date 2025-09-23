
namespace XYVR.AccountAuthority.Resonite;

internal class HashToSession
{
    private readonly HashSet<string> _sessionIds = new();
    private readonly Dictionary<string, SessionBrief> _sessionIdToSessionBrief = new();
    private readonly Dictionary<string, string> _mixedHashesToSessionId = new();
    private readonly ReaderWriterLockSlim _lock = new();

    public void SubmitSession(SessionBrief brief)
    {
        _lock.EnterWriteLock();
        try
        {
            _sessionIds.Add(brief.sessionId);
            _sessionIdToSessionBrief.TryAdd(brief.sessionId, brief);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public async Task<SessionBrief?> ResolveSession(string wantedHash, string salt)
    {
        _lock.EnterReadLock();
        try
        {
            if (_mixedHashesToSessionId.TryGetValue(wantedHash, out var value))
            {
                return _sessionIdToSessionBrief[value];
            }
        }
        finally
        {
            _lock.ExitReadLock();
        }

        _lock.EnterReadLock();
        string[] sessionIdsCopy;
        try
        {
            sessionIdsCopy = _sessionIds.ToArray();
        }
        finally
        {
            _lock.ExitReadLock();
        }

        foreach (var sessionId in sessionIdsCopy)
        {
            if (await ResoniteHash.Rehash(sessionId, salt) == wantedHash)
            {
                _lock.EnterWriteLock();
                try
                {
                    _mixedHashesToSessionId.TryAdd(wantedHash, sessionId);
                    return _sessionIdToSessionBrief.GetValueOrDefault(sessionId);
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
        }

        return null;
    }
}

public record SessionBrief
{
    public required string sessionId { get; init; }
    public required string sessionGuid { get; init; }
}