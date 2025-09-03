using XYVR.Core;
using XYVR.Scaffold;

namespace XYVR.Data.Collection;

// Caution: Can be called by different threads.
public class DataCollectionStorage : IDataCollector
{
    public DateTime GetCurrentTime()
    {
        return DateTime.Now;
    }
    
    public void Ingest(DataCollectionTrail trail)
    {
        Console.WriteLine($"{trail.timestamp} {trail.reason} {trail.apiSource} {trail.route} {trail.status} {trail.responseObject}");
        
        _ = Task.Run(async () => await WriteToJsonlFileAsync(trail));
    }

    private async Task WriteToJsonlFileAsync(DataCollectionTrail trail)
    {
        await Scaffolding.WriteToDataCollectionFile(trail);
    }
}