namespace XYVR.Core;

public class LiveStatusMonitoring
{
    private readonly Dictionary<NamedApp, Dictionary<string, ImmutableLiveUserUpdate>> _liveUpdatesByAppByUser = new();
    private readonly List<SessionRef> _sessions = new();
    private readonly Dictionary<string, SessionRef> _guidToSession = new();
    private readonly Dictionary<NamedApp, Dictionary<string, SessionRef>> _namedAppToInAppIdToSession = new();
    private readonly Dictionary<NamedApp, Dictionary<string, string>> _namedAppToAccountGuidToSessionParticipationGuid = new();

    private event LiveUserUpdateMerged? OnLiveUserUpdateMerged;
    public delegate Task LiveUserUpdateMerged(ImmutableLiveUserUpdate liveUpdate);

    private event LiveSessionUpdated? OnLiveSessionUpdated;
    public delegate Task LiveSessionUpdated(ImmutableLiveSession session);

    private class SessionRef(ImmutableLiveSession value)
    {
        internal string guid => value.guid;
        internal ImmutableLiveSession value = value;
    }

    public LiveStatusMonitoring()
    {
        foreach (var namedApp in Enum.GetValues<NamedApp>())
        {
            _liveUpdatesByAppByUser[namedApp] = new Dictionary<string, ImmutableLiveUserUpdate>();
            _namedAppToInAppIdToSession[namedApp] = new Dictionary<string, SessionRef>();
            _namedAppToAccountGuidToSessionParticipationGuid[namedApp] = new Dictionary<string, string>();
        }
    }

    public List<ImmutableLiveUserUpdate> GetAllUserData(NamedApp namedApp)
    {
        return _liveUpdatesByAppByUser[namedApp].Values.ToList();
    }

    public List<ImmutableLiveUserUpdate> GetAllUserData()
    {
        return _liveUpdatesByAppByUser.Values.SelectMany(it => it.Values).ToList();
    }

    public List<ImmutableLiveSession> GetAllSessions()
    {
        return _sessions.Select(it => it.value).ToList();
    }
    
    public async Task MergeUser(ImmutableLiveUserUpdate inputUpdate)
    {
        ImmutableLiveUserUpdate actualUpdate;
        bool updateWasModified;
        
        // TODO:
        // It is possible to have the same inAppIdentifier being updated with a different status,
        // if the caller has multiple connections on the same app.
        // For example, if someone's status is set to invisible, it may be possible that the status
        // of that account is shown as Offline on one connection, and Online/InSameInstance for another connection.
        // In that case, we may need to avoid deduplicating status by inAppIdentifier alone,
        // and also use the callerInAppIdentifier, to know where we got the status from.
        // The UI side or BFF would have to decide what status to associate with that account.
        if (_liveUpdatesByAppByUser[inputUpdate.namedApp].TryGetValue(inputUpdate.inAppIdentifier, out var existingUpdate))
        {
            var modifiedUpdate = existingUpdate;
            
            if (inputUpdate.onlineStatus != null) modifiedUpdate = modifiedUpdate with { onlineStatus = inputUpdate.onlineStatus };
            if (inputUpdate.customStatus != null) modifiedUpdate = modifiedUpdate with { customStatus = inputUpdate.customStatus };
            if (inputUpdate.mainSession != null) modifiedUpdate = modifiedUpdate with { mainSession = inputUpdate.mainSession };

            // Did anything actually change?
            if (modifiedUpdate != existingUpdate)
            {
                // The trigger is only relevant if an event actually causes any content to change
                modifiedUpdate = modifiedUpdate with { trigger = inputUpdate.trigger };
                
                _liveUpdatesByAppByUser[inputUpdate.namedApp][inputUpdate.inAppIdentifier] = modifiedUpdate;

                actualUpdate = modifiedUpdate;
                updateWasModified = true;
            }
            else
            {
                Console.WriteLine($"A LiveUpdate on {existingUpdate.inAppIdentifier} has resulted in no change (triggered by {inputUpdate.trigger}, there will be no OnLiveUserUpdateMerged emitted.");

                actualUpdate = existingUpdate;
                updateWasModified = false;
            }
        }
        else
        {
            _liveUpdatesByAppByUser[inputUpdate.namedApp][inputUpdate.inAppIdentifier] = inputUpdate;

            actualUpdate = inputUpdate;
            updateWasModified = true;
        }

        var participant = actualUpdate.AsParticipant();
        
        ImmutableLiveSession? liveSession;
        bool liveSessionChanged;
        ImmutableLiveSession? previousSession;
        if (inputUpdate.mainSession is { knowledge: LiveUserSessionKnowledge.Known })
        {
            var userKnownSession = inputUpdate.mainSession.knownSession!;
            var nonIndexedSession = new ImmutableNonIndexedLiveSession
            {
                namedApp = inputUpdate.namedApp,
                qualifiedAppName = inputUpdate.qualifiedAppName,
                inAppSessionIdentifier = userKnownSession.inAppSessionIdentifier,
                inAppSessionName = userKnownSession.inAppSessionName,
                inAppVirtualSpaceName = userKnownSession.inAppVirtualSpaceName,
                inAppHost = userKnownSession.inAppHost,
            };
            (liveSession, liveSessionChanged, previousSession) = InternalMergeSessionAndGet(nonIndexedSession, participant);
        }
        else
        {
            liveSession = null;
            liveSessionChanged = false;
            previousSession = null;
        }

        // We want to send the updates in a preferred order:
        // - Session first, so that the receiver side may register that session.
        // - User next, so that the receiver side may have a better luck associating the user with that session.
        // This isn't required.
        if (previousSession != null && OnLiveSessionUpdated != null)
        {
            await OnLiveSessionUpdated.Invoke(previousSession);
        }
        if (liveSession != null && liveSessionChanged && OnLiveSessionUpdated != null)
        {
            await OnLiveSessionUpdated.Invoke(liveSession);
        }
        if (updateWasModified && OnLiveUserUpdateMerged != null)
        {
            await OnLiveUserUpdateMerged.Invoke(inputUpdate);
        }
    }

    public async Task MergeSession(ImmutableNonIndexedLiveSession inputSession)
    {
        var (liveSession, changed, _) = InternalMergeSessionAndGet(inputSession);
        
        if (changed && OnLiveSessionUpdated != null)
        {
            await OnLiveSessionUpdated.Invoke(liveSession);
        }
    }

    private (ImmutableLiveSession, bool, ImmutableLiveSession?) InternalMergeSessionAndGet(ImmutableNonIndexedLiveSession inputSession, ImmutableParticipant? usingParticipant = null)
    {
        ImmutableLiveSession actualSession = null;
        bool outChanged;
        ImmutableLiveSession participantRemoval = null;
        
        if (_namedAppToInAppIdToSession[inputSession.namedApp].TryGetValue(inputSession.inAppSessionIdentifier, out var existingSession))
        {
            var modifiedSession = existingSession.value;
            
            if (inputSession.inAppSessionName != null) modifiedSession = modifiedSession with { inAppSessionName = inputSession.inAppSessionName };
            if (inputSession.inAppVirtualSpaceName != null) modifiedSession = modifiedSession with { inAppVirtualSpaceName = inputSession.inAppVirtualSpaceName };
            
            if (usingParticipant != null && !modifiedSession.participants.Contains(usingParticipant))
                modifiedSession = modifiedSession with { participants = [..modifiedSession.participants.Append(usingParticipant)] };

            var changed = modifiedSession != existingSession.value;
            if (changed)
            {
                existingSession.value = modifiedSession;
            }
            
            (actualSession, outChanged) = (modifiedSession, changed);
        }
        else
        {
            var liveSession = ImmutableNonIndexedLiveSession.MakeIndexed(inputSession);
            
            if (usingParticipant != null) liveSession = liveSession with { participants = [usingParticipant] };

            var sessionRef = new SessionRef(liveSession);
            _sessions.Add(sessionRef);
            _guidToSession[liveSession.guid] = sessionRef;
            _namedAppToInAppIdToSession[liveSession.namedApp][liveSession.inAppSessionIdentifier] = sessionRef;

            if (usingParticipant != null && usingParticipant.isKnown)
            {
                _namedAppToAccountGuidToSessionParticipationGuid[inputSession.namedApp][usingParticipant.knownAccount!.inAppIdentifier]
                    = liveSession.guid;
            }
            
            (actualSession, outChanged) = (liveSession, true);
        }

        if (usingParticipant is { isKnown: true })
        {
            if (_namedAppToAccountGuidToSessionParticipationGuid[inputSession.namedApp]
                    .TryGetValue(usingParticipant.knownAccount!.inAppIdentifier, out var existingParticipationGuid)
                && existingParticipationGuid != actualSession.guid)
            {
                var previous = _guidToSession[existingParticipationGuid].value;
                participantRemoval = previous with
                {
                    participants = [
                        ..previous.participants
                            .Where(participant => !participant.isKnown || participant.knownAccount!.inAppIdentifier != usingParticipant.knownAccount!.inAppIdentifier)
                    ]
                };
                _guidToSession[existingParticipationGuid].value = participantRemoval;
            }

            _namedAppToAccountGuidToSessionParticipationGuid[inputSession.namedApp][usingParticipant.knownAccount!.inAppIdentifier]
                = actualSession.guid;
        }

        return (actualSession, outChanged, participantRemoval);
    }

    public void AddUserUpdateMergedListener(LiveUserUpdateMerged listener)
    {
        OnLiveUserUpdateMerged -= listener;
        OnLiveUserUpdateMerged += listener;
    }

    public void RemoveUserUpdateMergedListener(LiveUserUpdateMerged listener)
    {
        OnLiveUserUpdateMerged -= listener;
    }

    public void AddSessionUpdatedListener(LiveSessionUpdated listener)
    {
        OnLiveSessionUpdated -= listener;
        OnLiveSessionUpdated += listener;
    }

    public void RemoveSessionUpdatedListener(LiveSessionUpdated listener)
    {
        OnLiveSessionUpdated -= listener;
    }

    public ImmutableLiveUserUpdate? GetLiveSessionStateOrNull(NamedApp accountNamedApp, string accountInAppIdentifier)
    {
        return _liveUpdatesByAppByUser[accountNamedApp].GetValueOrDefault(accountInAppIdentifier);
    }
}