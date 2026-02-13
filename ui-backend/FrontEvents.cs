namespace XYVR.UI.Backend;

public static class FrontEvents
{
    // Only for incremental updates. Reminder: Status from live updates is aggregated, but a status update does not update the individual itself.
    public const string EventForIndividualUpdated = "individualUpdated";

    // Only for incremental updates. This is called to update the ui while data collection is running.
    public const string EventForDataCollectionUpdated = "dataCollectionUpdated";

    // Only for live updates.
    public const string EventForLiveUpdateMerged = "liveUpdateMerged";
    public const string EventForLiveSessionUpdated = "liveSessionUpdated";
}