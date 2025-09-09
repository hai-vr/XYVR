namespace XYVR.Core;

public class ConnectorManagement
{
    public List<Connector> Connectors { get; }

    public ConnectorManagement(Connector[] connectors)
    {
        Connectors = connectors.ToList();
    }
    
    public Connector CreateNewConnector(ConnectorType connectorType)
    {
        var connector = new Connector
        {
            guid = XYVRGuids.ForConnector(),
            displayName = "",
            type = connectorType,
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
        existingConnector.refreshMode = connector.refreshMode;
        existingConnector.liveMode = connector.liveMode;
        existingConnector.account = connector.account;
    }

    public void DeleteConnector(string guid)
    {
        Connectors.Remove(Connectors.First(conn => conn.guid == guid));
    }

    public Connector GetConnector(string guid)
    {
        return Connectors.First(conn => conn.guid == guid);
    }
}