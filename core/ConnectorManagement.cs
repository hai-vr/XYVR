namespace XYVR.Core;

public class ConnectorManagement
{
    public List<Connector> Connectors { get; }

    public ConnectorManagement(Connector[] connectors)
    {
        Connectors = connectors.ToList();
    }
    
    public Connector CreateNewConnector()
    {
        var connector = new Connector
        {
            guid = Guid.NewGuid().ToString(),
            displayName = "",
            type = ConnectorType.Offline,
            refreshMode = RefreshMode.ContinuousFullUpdates,
            liveMode = LiveMode.OnlyInGameStatus,
            account = null
        };
        
        Connectors.Add(connector);

        return connector;
    }

    public void UpdateConnector(Connector connector)
    {
        var existingConnector = Connectors.First(conn => conn.guid == connector.guid);
        existingConnector.displayName = connector.displayName;
        existingConnector.type = connector.type;
        existingConnector.refreshMode = connector.refreshMode;
        existingConnector.liveMode = connector.liveMode;
        existingConnector.account = connector.account;
    }

    public void DeleteConnector(string guid)
    {
        Connectors.Remove(Connectors.First(conn => conn.guid == guid));
    }
}