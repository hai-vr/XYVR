namespace XYVR.Data.Collection.monitoring;

public interface ILiveMonitoring
{
    Task StartMonitoring();
    Task StopMonitoring();
    
    // In the monitoring responses, we need to know who is the caller account.
    // This is because the responses of two different monitoring sources can disagree on the status of a given account
    // (example: one caller is a contact of an account, but another caller is not a contact of that same account),
    // so we need some way to reconcile it.
    Task DefineCaller(string callerInAppIdentifier);
}