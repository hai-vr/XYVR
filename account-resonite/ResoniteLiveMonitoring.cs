using System.Diagnostics;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Immutable;
using XYVR.API.Audit;
using XYVR.Core;

namespace XYVR.AccountAuthority.Resonite;

public class ResoniteLiveMonitoring : ILiveMonitoring, IDisposable
{
    private readonly ICredentialsStorage _credentialsStorage;
    private readonly LiveStatusMonitoring _monitoring;
    private readonly string _uid__sensitive;
    
    private readonly SemaphoreSlim _operationLock = new(1, 1);
    private bool _isConnected;
    private string? _callerInAppIdentifier;
    
    private ResoniteLiveCommunicator? _liveComms;
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly HashToSession _hashToSession;

    public ResoniteLiveMonitoring(ICredentialsStorage credentialsStorage, LiveStatusMonitoring monitoring, string uid__sensitive)
    {
        _credentialsStorage = credentialsStorage;
        _monitoring = monitoring;
        _uid__sensitive = uid__sensitive;
        
        _hashToSession = new HashToSession();
    }

    public Task DefineCaller(string callerInAppIdentifier)
    {
        _callerInAppIdentifier = callerInAppIdentifier;
        return Task.CompletedTask;
    }

    public async Task StartMonitoring()
    {
        if (_callerInAppIdentifier == null) throw new InvalidOperationException("Caller must be defined to start monitoring");
        
        await _operationLock.WaitAsync();
        try
        {
            if (_isConnected) return;
            _cancellationTokenSource = new CancellationTokenSource();
            
            _liveComms = new ResoniteLiveCommunicator(_credentialsStorage, _callerInAppIdentifier, _uid__sensitive, new DoNotStoreAnythingStorage(), TryGetSessionIdToSessionGuid, _hashToSession);
            
            var serializer = new JsonSerializerSettings()
            {
                Converters = { new StringEnumConverter() }
            };
            
            var alreadyListeningTo = new HashSet<string>();
            _liveComms.OnLiveUpdateReceived += async update =>
            {
                // XYVRLogging.WriteLine(this, $"OnLiveUpdateReceived: {JsonConvert.SerializeObject(update, serializer)}");
                await _monitoring.MergeUser(update);

                if (!alreadyListeningTo.Contains(update.inAppIdentifier))
                {
                    await _liveComms.ListenOnContact(update.inAppIdentifier);
                    alreadyListeningTo.Add(update.inAppIdentifier);
                }
            };
            _liveComms.OnReconnected += async () =>
            {
                foreach (var inAppIdentifier in alreadyListeningTo)
                {
                    await _liveComms.ListenOnContact(inAppIdentifier);
                }
            };
            _liveComms.OnSessionUpdated += WhenSessionUpdated;
            
            await _liveComms.Connect();
            
            _ = Task.Run(BackgroundTask, _cancellationTokenSource.Token);
            _isConnected = true;
        }
        finally
        {
            _operationLock.Release();
        }
    }

    private async void WhenSessionUpdated(SessionUpdateJsonObject sessionUpdate)
    {
        var sessionId = sessionUpdate.sessionId;

        // As a precaution, we only accept thumbnail URLs if they point to resonite.com or any subdomain of it
        // to prevent a possible violation of privacy as we don't know how much we can trust the incoming data.
        var sanitizedThumbnailUrl = sessionUpdate.thumbnailUrl != null ? EnsureUrlIsResoniteDotComOrNull(sessionUpdate.thumbnailUrl) : null;

        var markers = ToMarker(sessionUpdate.accessLevel);
        if (sessionUpdate.headlessHost) markers.Add(LiveSessionMarker.ResoniteHeadless);
        
        var correspondingSession = await _monitoring.MergeSession(new ImmutableNonIndexedLiveSession
        {
            namedApp = NamedApp.Resonite,
            qualifiedAppName = ResoniteCommunicator.ResoniteQualifiedAppName,
            inAppVirtualSpaceName = ResoniteLiveCommunicator.ExtractTextFromColorTags(sessionUpdate.name),
            inAppSessionIdentifier = sessionId,
            inAppHost = new ImmutableLiveSessionHost { inAppHostIdentifier = sessionUpdate.hostUserId, inAppHostDisplayName = sessionUpdate.hostUsername },
            currentAttendance = sessionUpdate.joinedUsers,
            sessionCapacity = sessionUpdate.maxUsers,
            virtualSpaceDefaultCapacity = sessionUpdate.maxUsers,
            thumbnailUrl = sanitizedThumbnailUrl,

            allParticipants = sessionUpdate.sessionUsers
                .Select(x=>new ImmutableParticipant {
                    isHost = sessionUpdate.hostUserId != null ? 
                        sessionUpdate.hostUserId == x.userID
                        : sessionUpdate.hostUsername == x.username,
                    isKnown = false, 
                    unknownAccount = new() { 
                        inAppDisplayName = x.username,
                        inAppIdentifier = x.userID
                    } 
                }
            ).ToImmutableArray(),
            
            markers = [..markers],

            callerInAppIdentifier = _callerInAppIdentifier!
        });
        
        _hashToSession.SubmitSession(new SessionBrief
        {
            sessionId = sessionId,
            sessionGuid = correspondingSession.guid,
        });

        foreach (var userUpdate in _monitoring.GetAllUserData(NamedApp.Resonite))
        {
            var specifics = (ImmutableResoniteLiveSessionSpecifics)userUpdate.sessionSpecifics!;
            if (specifics.userHashSalt != null)
            {
                var modifiedUserUpdate = userUpdate;
                
                string rehash = null;
                if (userUpdate.mainSession?.knowledge == LiveUserSessionKnowledge.KnownButNoData)
                {
                    rehash = await ResoniteHash.Rehash(sessionId, specifics.userHashSalt);
                    if (specifics is { sessionHash: not null, userHashSalt: not null } && rehash == specifics.sessionHash)
                    {
                        XYVRLogging.WriteLine(this, "Received a session for which a user had an unresolved hash for.");

                        modifiedUserUpdate = modifiedUserUpdate with { mainSession = new ImmutableLiveUserSessionState { knowledge = LiveUserSessionKnowledge.Known, sessionGuid = correspondingSession.guid } };
                    }
                }

                if (specifics.sessionHashes.Length != userUpdate.multiSessionGuids.Length && specifics.userHashSalt != null)
                {
                    var sessionGuids = await ResolveSessionGuids(specifics);
                    modifiedUserUpdate = modifiedUserUpdate with { multiSessionGuids = [..sessionGuids] };
                }

                if (modifiedUserUpdate != userUpdate)
                {
                    await _monitoring.MergeUser(modifiedUserUpdate);
                }
            }
        }
    }

    private List<LiveSessionMarker> ToMarker(string sessionUpdateAccessLevel)
    {
        return sessionUpdateAccessLevel switch
        {
            "Anyone" => [LiveSessionMarker.ResoniteAnyone],
            "RegisteredUsers" => [LiveSessionMarker.ResoniteRegisteredUsers],
            "ContactsPlus" => [LiveSessionMarker.ResoniteContactsPlus],
            "Contacts" => [LiveSessionMarker.ResoniteContacts],
            "LAN" => [LiveSessionMarker.ResoniteLAN],
            "Private" => [LiveSessionMarker.ResonitePrivate],
            _ => []
        };
    }

    private static string? EnsureUrlIsResoniteDotComOrNull(string thumbnailUrl)
    {
        if (!thumbnailUrl.StartsWith("https://")) return null;

        if (Uri.TryCreate(thumbnailUrl, UriKind.Absolute, out var uri))
        {
            var host = uri.Host.ToLowerInvariant();

            if (host == AuditUrls.ResoniteSessionThumbnailsPermittedHostAndSubdomainHost || host.EndsWith($".{AuditUrls.ResoniteSessionThumbnailsPermittedHostAndSubdomainHost}"))
            {
                return thumbnailUrl;
            }
        }
                
        return null;
    }


    private async Task<List<string>> ResolveSessionGuids(ImmutableResoniteLiveSessionSpecifics specifics)
    {
        var sessionGuids = new List<string>();
        foreach (var wantedHash in specifics.sessionHashes)
        {
            var session = await _hashToSession.ResolveSession(wantedHash, specifics.userHashSalt);
            if (session != null)
            {
                sessionGuids.Add(session.sessionGuid);
            }
        }

        return sessionGuids;
    }

    private string? TryGetSessionIdToSessionGuid(string sessionId)
    {
        return _monitoring.GetAllSessions(NamedApp.Resonite)
            .FirstOrDefault(session => session.inAppSessionIdentifier == sessionId)?.guid;
    }

    private async Task BackgroundTask()
    {
        try
        {
            while (true) // Canceled by token
            {
                // Unsure why, but if it runs for a while, we won't receive any updates until the user actually starts the game?
                // Request a full update every so often
                await Task.Delay(TimeSpan.FromMinutes(1), _cancellationTokenSource.Token);
                await _liveComms.RequestFullUpdate();
            }
        }
        catch (Exception e)
        {
            XYVRLogging.ErrorWriteLine(this, e);
            throw;
        }
    }

    public async Task StopMonitoring()
    {
        await _operationLock.WaitAsync();
        try
        {
            if (!_isConnected) return;
            
            XYVRLogging.WriteLine(this, "Will try to cancel token");
            // await _cancellationTokenSource.CancelAsync();
            _cancellationTokenSource!.CancelAsync(); // FIXME: we have a problem when we wait for this to finish, it never completes. Why?
            XYVRLogging.WriteLine(this, "Token cancelled. Will try to disconnect");
            
            await _liveComms!.Disconnect();
            XYVRLogging.WriteLine(this, "Disconnected.");
            
            _liveComms = null;
            _cancellationTokenSource = null;
            _isConnected = false;
        }
        finally
        {
            _operationLock.Release();
        }
    }

    public void Dispose()
    {
        _operationLock.Dispose();
    }

    public Task MakeGameClientJoinOrSelfInvite(string sessionId, CancellationTokenSource cancellationTokenSource)
    {
        DANGER_OpenResoniteSession(sessionId);
        return Task.CompletedTask;
    }

    private static void DANGER_OpenResoniteSession(string sessionId)
    {
        if (!sessionId.StartsWith("S-"))
        {
            throw new ArgumentException("Invalid session ID format. Expected format: S-...", nameof(sessionId));
        }
        
        var url = $"resonite:?session=ressession:///{sessionId}";

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Process.Start(new ProcessStartInfo
            {
                // SECURITY: Don't allow any URL here. Otherwise, this can cause a RCE.
                FileName = url,
                UseShellExecute = true
            });
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // SECURITY: URL is an argument to xdg-open. Do not modify the code to pass it to the shell or something, that would enable possible RCEs.
            Process.Start("xdg-open", url);
        }
    }
}