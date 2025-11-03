using System.Runtime.InteropServices;
using Newtonsoft.Json;
using XYVR.Core;
using XYVR.Scaffold;

namespace XYVR.UI.Backend;

[ComVisible(true)]
public interface ILiveBFF
{
    string GetAllExistingLiveUserData();
    string GetAllExistingLiveSessionData();
    Task MakeGameClientJoinOrSelfInvite(string appName, string inAppIdentifier, string sessionId);
}

[ComVisible(true)]
[ClassInterface(ClassInterfaceType.None)]
public class LiveBFF : ILiveBFF
{
    private readonly AppLifecycle _appLifecycle;
    private readonly JsonSerializerSettings _serializer;
    private readonly VRChatThumbnailCache _thumbnailCache;

    private HashSet<string> _doWeCareAboutThisSessionGuid = new();

    public LiveBFF(AppLifecycle appLifecycle)
    {
        _appLifecycle = appLifecycle;
        _serializer = BFFUtils.NewSerializer();
        _thumbnailCache = Scaffolding.ThumbnailCache();
    }

    public string GetAllExistingLiveUserData()
    {
        try
        {
            var liveData = _appLifecycle.LiveStatusMonitoring.GetAllUserData()
                .Select(update => FrontLiveUserUpdate.FromCore(update, _appLifecycle.LiveStatusMonitoring))
                .ToList();
            return JsonConvert.SerializeObject(liveData, _serializer);
        }
        catch (Exception e)
        {
            XYVRLogging.ErrorWriteLine(this, e);
            throw;
        }
    }

    public string GetAllExistingLiveSessionData()
    {
        try
        {
            var liveData = _appLifecycle.LiveStatusMonitoring.GetAllSessions()
                .Select(FrontLiveSession.FromCore)
                .ToList();
            return JsonConvert.SerializeObject(liveData, _serializer);
        }
        catch (Exception e)
        {
            XYVRLogging.ErrorWriteLine(this, e);
            throw;
        }
    }

    public async Task<byte[]?> GetThumbnailBytesOrNull(string sha__mustNotContainPathTraversal)
    {
        return await _thumbnailCache.GetByShaOrNull(sha__mustNotContainPathTraversal);
    }
    
    public async Task StartMonitoring()
    {
        var monitoring = _appLifecycle.LiveStatusMonitoring;
        monitoring.AddUserUpdateMergedListener(WhenUserUpdateMerged);
        monitoring.AddSessionUpdatedListener(WhenSessionUpdated);

        await _appLifecycle.LiveMonitoringAgent.StartMonitoring();
    }

    public async Task StopMonitoring()
    {
        var monitoring = _appLifecycle.LiveStatusMonitoring;
        monitoring.RemoveUserUpdateMergedListener(WhenUserUpdateMerged);
        monitoring.RemoveSessionUpdatedListener(WhenSessionUpdated);
        
        await _appLifecycle.LiveMonitoringAgent.StopMonitoring();
    }
    
    private async Task WhenUserUpdateMerged(ImmutableLiveUserUpdate update)
    {
        var live = _appLifecycle.LiveStatusMonitoring;
        await _appLifecycle.SendEventToReact(FrontEvents.EventForLiveUpdateMerged, FrontLiveUserUpdate.FromCore(update, live));
    }

    private async Task WhenSessionUpdated(ImmutableLiveSession session)
    {
        if (session.participants.Length == 0)
        {
            if (!_doWeCareAboutThisSessionGuid.Contains(session.guid))
            {
                return;
            }

            _doWeCareAboutThisSessionGuid.Remove(session.guid);
            XYVRLogging.WriteLine(this, $"All known participants have left {session.inAppVirtualSpaceName} in {session.namedApp}, so we will stop monitoring it (now monitoring {_doWeCareAboutThisSessionGuid.Count} sessions).");
        }
        else
        {
            var added = _doWeCareAboutThisSessionGuid.Add(session.guid);
            if (added)
            {
                XYVRLogging.WriteLine(this, $"We have just started monitoring {session.inAppVirtualSpaceName} in {session.namedApp} (now monitoring {_doWeCareAboutThisSessionGuid.Count} sessions).");
            }
        }
        
        await _appLifecycle.SendEventToReact(FrontEvents.EventForLiveSessionUpdated, FrontLiveSession.FromCore(session));

        var live = _appLifecycle.LiveStatusMonitoring;
        foreach (var userOfThatSession in live.GetAllUserData(session.namedApp)
                     .Where(update => update?.mainSession?.sessionGuid == session.guid))
        {
            await _appLifecycle.SendEventToReact(FrontEvents.EventForLiveUpdateMerged, FrontLiveUserUpdate.FromCore(userOfThatSession, live));
        }
    }

    public async Task OnClosed()
    {
        await Task.Run(async () =>
        {
            try
            {
                await StopMonitoring();
            }
            catch (Exception e)
            {
                XYVRLogging.ErrorWriteLine(this, e);
                throw;
            }
        });
    }

    public async Task MakeGameClientJoinOrSelfInvite(string appName, string inAppIdentifier, string sessionId)
    {
        if (Enum.TryParse<NamedApp>(appName, out var namedApp))
        {
            await _appLifecycle.LiveMonitoringAgent.MakeGameClientJoinOrSelfInvite(namedApp, inAppIdentifier, sessionId);
        }
    }
}