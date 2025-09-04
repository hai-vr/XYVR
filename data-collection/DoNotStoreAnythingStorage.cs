using XYVR.Core;

namespace XYVR.Data.Collection;

internal class DoNotStoreAnythingStorage : IResponseCollector
{
    public DateTime GetCurrentTime() { return DateTime.Now; }
    public void Ingest(ResponseCollectionTrail response) { }
}