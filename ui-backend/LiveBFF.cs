using System.Runtime.InteropServices;
using Newtonsoft.Json;
using XYVR.Core;

namespace XYVR.UI.Backend;

[ComVisible(true)]
public interface ILiveBFF
{
    string GetAllExistingLiveUserData();
    string GetAllExistingLiveSessionData();
}

[ComVisible(true)]
[ClassInterface(ClassInterfaceType.None)]
public class LiveBFF : ILiveBFF
{
    private readonly AppLifecycle _appLifecycle;
    private readonly JsonSerializerSettings _serializer;

    private List<ILiveMonitoring>? _liveMonitoringAgents;
    private HashSet<string> _doWeCareAboutThisSessionGuid = new();

    public LiveBFF(AppLifecycle appLifecycle)
    {
        _appLifecycle = appLifecycle;
        _serializer = BFFUtils.NewSerializer();
    }
    
    public string GetAllExistingLiveUserData()
    {
        try
        {
            var liveData = _appLifecycle.LiveStatusMonitoring.GetAllUserData();
            return JsonConvert.SerializeObject(liveData, _serializer);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public string GetAllExistingLiveSessionData()
    {
        try
        {
            var liveData = _appLifecycle.LiveStatusMonitoring.GetAllSessions();
            return JsonConvert.SerializeObject(liveData, _serializer);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task StartMonitoring()
    {
        if (_liveMonitoringAgents != null) return;
        
        var connectors = _appLifecycle.ConnectorsMgt;
        var credentials = _appLifecycle.CredentialsMgt;
        var monitoring = _appLifecycle.LiveStatusMonitoring;

        monitoring.AddUserUpdateMergedListener(WhenUserUpdateMerged);
        monitoring.AddSessionUpdatedListener(WhenSessionUpdated);
        
        ILiveMonitoring?[] liveMonitorings = await Task.WhenAll(connectors.Connectors
            .Where(connector => connector.liveMode != LiveMode.NoLiveFunction)
            .Select(async connector => await credentials.GetConnectedLiveMonitoringOrNull(connector, monitoring))
            .ToList());
        
        _liveMonitoringAgents = liveMonitorings
            .Where(collection => collection != null)
            .Cast<ILiveMonitoring>()
            .ToList();
        
        foreach (var agent in _liveMonitoringAgents)
        {
            await agent.StartMonitoring();
        }
    }

    public async Task StopMonitoring()
    {
        if (_liveMonitoringAgents == null) return;

        var monitoring = _appLifecycle.LiveStatusMonitoring;
        
        monitoring.RemoveUserUpdateMergedListener(WhenUserUpdateMerged);
        monitoring.RemoveSessionUpdatedListener(WhenSessionUpdated);
        
        var tasks = _liveMonitoringAgents.Select(agent =>
        {
            return Task.Run(async () =>
            {
                try
                {
                    Console.WriteLine($"Stopping monitoring of {agent.GetType().Name}");
                    await agent.StopMonitoring();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            });
        }).ToList();
        
        await Task.WhenAll(tasks);

        _liveMonitoringAgents = null;
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
            if (_doWeCareAboutThisSessionGuid.Contains(session.guid))
            {
                Console.WriteLine($"We no longer care about session: {session.inAppVirtualSpaceName}");
                _doWeCareAboutThisSessionGuid.Remove(session.guid);
            }
            else
            {
                Console.WriteLine($"We do not care about session: {session.inAppVirtualSpaceName}");
                return;
            }
        }
        _doWeCareAboutThisSessionGuid.Add(session.guid);
        
        await _appLifecycle.SendEventToReact(FrontEvents.EventForLiveSessionUpdated, FrontLiveSession.FromCore(session));

        var live = _appLifecycle.LiveStatusMonitoring;
        foreach (var userOfThatSession in live.GetAllUserData(session.namedApp)
                     .Where(update => update?.mainSession?.sessionGuid == session.guid))
        {
            await _appLifecycle.SendEventToReact(FrontEvents.EventForLiveUpdateMerged, FrontLiveUserUpdate.FromCore(userOfThatSession, live));
        }
    }

    public void OnClosed()
    {
        // FIXME: We start a task because we're having an issue cancelling the task. So: stop monitoring without awaiting, wait a second, then close.
        Task.Run(async () =>
        {
            StopMonitoring();
            await Task.Delay(1000);
            // Close for real
        }).Wait();
    }
}