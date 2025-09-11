using XYVR.Core;
using XYVR.Data.Collection;

namespace XYVR.Scaffold;

// Caution: Can be called by different threads.
public class ResponseCollectionStorage : IResponseCollector
{
    public DateTime GetCurrentTime()
    {
        return DateTime.Now;
    }
    
    public void Ingest(ResponseCollectionTrail trail)
    {
        _ = Task.Run(async () => await WriteToJsonlFileAsync(trail));
    }

    private async Task WriteToJsonlFileAsync(ResponseCollectionTrail trail)
    {
        await Scaffolding.WriteToResponseCollectionFile(trail);
    }
}