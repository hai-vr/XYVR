using System.Runtime.InteropServices;
using Newtonsoft.Json;
using XYVR.AccountAuthority.Resonite;
using XYVR.Core;
using XYVR.Data.Collection.monitoring;

namespace XYVR.UI.WebviewUI;

[ComVisible(true)]
public interface ILiveBFF
{
    string GetAllExistingLiveData();
}

[ComVisible(true)]
[ClassInterface(ClassInterfaceType.None)]
public class LiveBFF : ILiveBFF
{
    private readonly MainWindow _mainWindow;
    private readonly JsonSerializerSettings _serializer;

    private List<ILiveMonitoring>? _liveMonitoringAgents;


    public LiveBFF(MainWindow mainWindow)
    {
        _mainWindow = mainWindow;
        _serializer = BFFUtils.NewSerializer();
    }
    
    public string GetAllExistingLiveData()
    {
        var liveData = _mainWindow.LiveStatusMonitoring.GetAll();
        return JsonConvert.SerializeObject(liveData, _serializer);
    }

    public async Task StartMonitoring()
    {
        if (_liveMonitoringAgents != null) return;
        
        var connectors = _mainWindow.ConnectorsMgt;
        var credentials = _mainWindow.CredentialsMgt;
        var monitoring = _mainWindow.LiveStatusMonitoring;

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

        var monitoring = _mainWindow.LiveStatusMonitoring;
        
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
        await _mainWindow.SendEventToReact(FrontEvents.EventForLiveUpdateMerged, FrontLiveUserUpdate.FromCore(update));
    }

    private async Task WhenSessionUpdated(LiveSession session)
    {
        await _mainWindow.SendEventToReact(FrontEvents.EventForLiveSessionUpdated, FrontLiveSession.FromCore(session, _mainWindow.LiveStatusMonitoring));
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