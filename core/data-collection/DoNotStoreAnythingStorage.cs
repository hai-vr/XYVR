namespace XYVR.Core;

public class DoNotStoreAnythingStorage : IResponseCollector
{
    public DateTime GetCurrentTime() { return DateTime.Now; }
    public void Ingest(ResponseCollectionTrail response) { }
}