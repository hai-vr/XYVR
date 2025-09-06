namespace XYVR.Data.Collection.monitoring;

public interface ILiveMonitoring
{
    Task StartMonitoring();
    Task StopMonitoring();
    Task DefineCaller(string callerInAppIdentifier);
}