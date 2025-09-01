using System.Collections.Concurrent;
using XYVR.Core;

namespace XYVR.Data.Collection;

// Caution: Can be called by different threads.
public class DataCollectionStorage : IDataCollector
{
    private readonly ConcurrentQueue<DataCollectionTrail> _data = new();

    public DateTime GetCurrentTime()
    {
        return DateTime.Now;
    }
    
    public void Ingest(DataCollectionTrail trail)
    {
        _data.Enqueue(trail);
        
        // FIXME: Remove this
        Console.WriteLine($"{trail.timestamp} {trail.reason} {trail.apiSource} {trail.route} {trail.status} {trail.responseObject}");
    }
}