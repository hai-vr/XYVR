namespace XYVR.Core;

public interface IResponseCollector
{
    public DateTime GetCurrentTime();
    void Ingest(ResponseCollectionTrail response);
}