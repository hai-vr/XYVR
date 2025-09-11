namespace XYVR.Core
{
    public class ResponseCollectionTrail
    {
        public DateTime timestamp;
        public string trailGuid;
        public string requestGuid;

        public DataCollectionReason reason;
        public string apiSource;
        public string route;

        public DataCollectionResponseStatus status;
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
}