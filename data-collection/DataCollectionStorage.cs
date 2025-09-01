using System.Collections.Concurrent;
using System.Text;
using XYVR.Core;
using XYVR.Scaffold;

namespace XYVR.Data.Collection;

// Caution: Can be called by different threads.
public class DataCollectionStorage : IDataCollector
{
    private readonly SemaphoreSlim _fileLock = new(1, 1);
    
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
        
        _ = Task.Run(async () => await WriteToJsonlFileAsync(trail));
    }

    private async Task WriteToJsonlFileAsync(DataCollectionTrail trail)
    {
        await _fileLock.WaitAsync();
        try
        {
            var jsonLine = Scaffolding.SerializeAsSingleLine(trail);
            await File.AppendAllTextAsync(Scaffolding.DataCollectionFileName, jsonLine + Environment.NewLine, Encoding.UTF8);
        }
        finally
        {
            _fileLock.Release();
        }
    }
}