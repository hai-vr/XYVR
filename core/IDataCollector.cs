using XYVR.Data.Collection;

namespace XYVR.Core;

public interface IDataCollector
{
    public DateTime GetCurrentTime();
    void Ingest(DataCollectionTrail data);
}