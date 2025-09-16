using XYVR.API.Resonite;

namespace XYVR.AccountAuthority.Resonite;

internal class HashToSession
{
    private readonly HashSet<string> _sessionIds = new();
    private readonly Dictionary<string, SessionBrief> _sessionIdToSessionBrief = new();
    private readonly Dictionary<string, string> _mixedHashesToSessionId = new();

    public void SubmitSession(SessionBrief brief)
    {
        _sessionIds.Add(brief.sessionId);
        _sessionIdToSessionBrief.TryAdd(brief.sessionId, brief);
    }

    public async Task<SessionBrief?> ResolveSession(string wantedHash, string salt)
    {
        if (_mixedHashesToSessionId.TryGetValue(wantedHash, out var value))
        {
            return _sessionIdToSessionBrief[value];
        }
        
        foreach (var sessionId in _sessionIds)
        {
            if (await ResoniteHash.Rehash(sessionId, salt) == wantedHash)
            {
                _mixedHashesToSessionId.TryAdd(wantedHash, sessionId);
                return _sessionIdToSessionBrief[sessionId];
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