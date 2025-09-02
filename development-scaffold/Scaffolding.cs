using System.Diagnostics;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using XYVR.Core;
using XYVR.Data.Collection;

namespace XYVR.Scaffold;

public static class Scaffolding
{
    private const string IndividualsJsonFileName = "individuals.json";
    private const string ConnectorsJsonFileName = "connectors.json";
    public const string DataCollectionFileName = "data-collection.jsonl";
    
    private static readonly Encoding Encoding = Encoding.UTF8;
    
    private static readonly JsonSerializerSettings Serializer = new()
    {
        Converters = { new StringEnumConverter() }
    };

    public static async Task<Individual[]> OpenRepository() => await OpenIfExists<Individual[]>(IndividualsJsonFileName, () => []);
    public static async Task SaveRepository(IndividualRepository repository) => await SaveTo(repository.Individuals, IndividualsJsonFileName);
    
    public static async Task<Connector[]> OpenConnectors() => await OpenIfExists<Connector[]>(ConnectorsJsonFileName, () => []);
    public static async Task SaveConnectors(ConnectorManagement management) => await SaveTo(management.Connectors, ConnectorsJsonFileName);

    public static string SerializeAsSingleLine(DataCollectionTrail trail)
    {
        return JsonConvert.SerializeObject(trail, Formatting.None, Serializer);
    }

    public static async Task<List<DataCollectionTrail>> RebuildTrail()
    {
        if (!File.Exists(DataCollectionFileName)) return [];
        
        var results = new List<DataCollectionTrail>();
        
        var lines = await File.ReadAllLinesAsync(DataCollectionFileName, Encoding);
        foreach (var line in lines)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                results.Add(JsonConvert.DeserializeObject<DataCollectionTrail>(line, Serializer));
            }
        }

        return results;
    }

    private static async Task<T> OpenIfExists<T>(string fileName, Func<T> defaultGen)
    {
        return File.Exists(fileName)
            ? JsonConvert.DeserializeObject<T>(await File.ReadAllTextAsync(fileName, Encoding), Serializer)!
            : defaultGen();
    }

    private static async Task SaveTo(object element, string fileName)
    {
        // FIXME: If the disk is full, this WILL corrupt the data that already exists, causing irrepairable loss.
        var serialized = JsonConvert.SerializeObject(element, Formatting.Indented, Serializer);
        await File.WriteAllTextAsync(fileName, serialized, Encoding);
    }

    public static void DANGER_OpenUrl(string url)
    {
        var isHttp = url.ToLowerInvariant().StartsWith("https://") || url.ToLowerInvariant().StartsWith("http://");
        if (!isHttp) throw new Exception("URL must be HTTP or HTTPS. This must be caught by the caller, not here!");
        
        Process.Start(new ProcessStartInfo
        {
            // SECURITY: Don't allow any URL here. Otherwise, this can cause a RCE.
            FileName = url,
            UseShellExecute = true
        });
    }
}