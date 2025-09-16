namespace XYVR.Core;

public class ResponseCollectionTrail
{
    public required DateTime timestamp;
    public required string trailGuid;
    public required string requestGuid;

    public required DataCollectionReason reason;
    public required string apiSource;
    public required string route;

    public required DataCollectionResponseStatus status;
    public object? responseObject;
    public object? metaObject;
}

public enum DataCollectionReason
{
    ManualRequest,
    CollectCallerAccount,
    FindUndiscoveredAccounts,
    CollectUndiscoveredAccount,
    CollectExistingAccount,
    CollectSessionLocationInformation
}

public enum DataCollectionResponseStatus
{
    Success,
    NotFound,
    Failure
}