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

        monitoring.AddMergeListener(WhenLiveUpdateMerged);
        
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
        monitoring.RemoveListener(WhenLiveUpdateMerged);
        foreach (var agent in _liveMonitoringAgents)
        {
            await agent.StopMonitoring();
        }

        _liveMonitoringAgents = null;
    }

    private async Task WhenLiveUpdateMerged(LiveUpdate update)
    {
        await _mainWindow.SendEventToReact("liveUpdateMerged", update);
    }

    public void OnClosed()
    {
        StopMonitoring().Wait();
    }
}