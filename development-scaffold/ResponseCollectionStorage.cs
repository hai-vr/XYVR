using XYVR.Core;

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
        _ = Task.Run(async () =>
        {
            try
            {
                await WriteToJsonlFileAsync(trail);
            }
            catch (Exception e)
            {
                XYVRLogging.WriteLine(this, e);
                throw;
            }
        });
    }

    private async Task WriteToJsonlFileAsync(ResponseCollectionTrail trail)
    {
        await Scaffolding.WriteToResponseCollectionFile(trail);
    }
}