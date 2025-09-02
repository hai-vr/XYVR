using XYVR.Core;

namespace XYVR.Data.Collection;

internal class DoNotStoreAnythingStorage : IDataCollector
{
    public DateTime GetCurrentTime() { return DateTime.Now; }
    public void Ingest(DataCollectionTrail data) { }
}