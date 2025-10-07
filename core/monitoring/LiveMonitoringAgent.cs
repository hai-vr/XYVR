using XYVR.Data.Collection;

namespace XYVR.Core;

public class LiveMonitoringAgent
{
    private readonly ConnectorManagement _connectors;
    private readonly CredentialsManagement _credentials;
    private readonly LiveStatusMonitoring _monitoring;
    
    private List<ILiveMonitoring>? _liveMonitoringAgents;

    public LiveMonitoringAgent(ConnectorManagement connectors, CredentialsManagement credentials, LiveStatusMonitoring monitoring)
    {
        _connectors = connectors;
        _credentials = credentials;
        _monitoring = monitoring;
    }

    public async Task StartMonitoring()
    {
        try
        {
            if (_liveMonitoringAgents != null) return;
        
            ILiveMonitoring?[] liveMonitorings = await Task.WhenAll(_connectors.Connectors
                .Where(connector => connector.liveMode != LiveMode.NoLiveFunction)
                .Select(async connector => await _credentials.GetConnectedLiveMonitoringOrNull(connector, _monitoring))
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
        catch (Exception e)
        {
            XYVRLogging.ErrorWriteLine(this, e);
            throw;
        }
    }

    public async Task StopMonitoring()
    {
        if (_liveMonitoringAgents == null) return;
        
        var tasks = _liveMonitoringAgents.Select(agent =>
        {
            return Task.Run(async () =>
            {
                try
                {
                    XYVRLogging.WriteLine(this, $"Stopping monitoring of {agent.GetType().Name}");
                    await agent.StopMonitoring();
                }
                catch (Exception e)
                {
                    XYVRLogging.ErrorWriteLine(this, e);
                    throw;
                }
            });
        }).ToList();
        
        await Task.WhenAll(tasks);

        _liveMonitoringAgents = null;
    }
}