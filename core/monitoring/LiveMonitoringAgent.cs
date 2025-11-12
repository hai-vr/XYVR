using XYVR.Data.Collection;

namespace XYVR.Core;

public class LiveMonitoringAgent
{
    private readonly ConnectorManagement _connectors;
    private readonly CredentialsManagement _credentials;
    private readonly LiveStatusMonitoring _monitoring;
    private readonly CancellationTokenSource _cancellationTokenSource;

    private Dictionary<string, ILiveMonitoring>? _liveMonitoringAgents;

    public LiveMonitoringAgent(ConnectorManagement connectors, CredentialsManagement credentials, LiveStatusMonitoring monitoring, CancellationTokenSource cancellationTokenSource)
    {
        _connectors = connectors;
        _credentials = credentials;
        _monitoring = monitoring;
        _cancellationTokenSource = cancellationTokenSource;
    }
    
    private record GuidToLiveMonitoring(string guid, ILiveMonitoring? liveMonitoring);

    public async Task StartMonitoring()
    {
        try
        {
            if (_liveMonitoringAgents != null) return;
        
            GuidToLiveMonitoring[] guidToLiveMonitorings = await Task.WhenAll(_connectors.Connectors
                .Where(connector => connector.liveMode != LiveMode.NoLiveFunction)
                .Select(async connector => new GuidToLiveMonitoring(connector.guid, await _credentials.GetConnectedLiveMonitoringOrNull(connector, _monitoring)))
                .ToList());

            _liveMonitoringAgents = guidToLiveMonitorings
                .Where(guidToLiveMonitoring => guidToLiveMonitoring.liveMonitoring != null)
                .ToDictionary(monitoring => monitoring.guid, monitoring => monitoring.liveMonitoring!);

            _credentials.OnConnectionConfirmed += WhenConnectionConfirmed;
            _credentials.OnLoggedOut += WhenLoggedOut;
        
            foreach (var agent in _liveMonitoringAgents.Values)
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

    private async Task WhenConnectionConfirmed(Connector connector)
    {
        if (_liveMonitoringAgents == null) return;
        
        if (connector.liveMode == LiveMode.NoLiveFunction) return;

        if (_liveMonitoringAgents.ContainsKey(connector.guid)) return;
        
        XYVRLogging.WriteLine(this, $"Connection confirmed on connection GUID {connector.guid}, will attempt to start monitoring it");

        var agent = await _credentials.GetConnectedLiveMonitoringOrNull(connector, _monitoring);
        if (agent == null) return;
        
        _liveMonitoringAgents[connector.guid] = agent;
        await agent.StartMonitoring();
    }

    private async Task WhenLoggedOut(Connector connector)
    {
        if (_liveMonitoringAgents == null) return;

        if (_liveMonitoringAgents.TryGetValue(connector.guid, out var agent))
        {
            XYVRLogging.WriteLine(this, $"Logged out ofGUID {connector.guid}, will stop monitoring it");
            
            await agent.StopMonitoring();
            _liveMonitoringAgents.Remove(connector.guid);
        }
    }

    public async Task StopMonitoring()
    {
        if (_liveMonitoringAgents == null) return;

        _credentials.OnConnectionConfirmed -= WhenConnectionConfirmed;
        _credentials.OnLoggedOut -= WhenLoggedOut;
        
        var tasks = _liveMonitoringAgents.Values.Select(agent =>
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

    public async Task MakeGameClientJoinOrSelfInvite(NamedApp namedApp, string inAppIdentifier, string sessionId)
    {
        var connector = _connectors.Connectors
            .Where(connector => connector.liveMode != LiveMode.NoLiveFunction)
            .FirstOrDefault(connector => connector.account?.namedApp == namedApp && connector.account?.inAppIdentifier == inAppIdentifier);

        var liveMonitoring = await _credentials.GetConnectedLiveMonitoringOrNull(connector, _monitoring);
        if (liveMonitoring == null) return;
        
        await liveMonitoring.MakeGameClientJoinOrSelfInvite(sessionId, _cancellationTokenSource);
    }
}