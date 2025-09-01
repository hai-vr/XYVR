using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using XYVR.Core;
using XYVR.Data.Collection;

namespace XYVR.Scaffold;

public static class Scaffolding
{
    private const string IndividualsJsonFileName = "individuals.json";
    public const string DataCollectionFileName = "data-collection.jsonl";
    
    private static readonly Encoding Encoding = Encoding.UTF8;
    
    private static JsonSerializerSettings? _serializer;

    public static async Task<Individual[]> OpenRepository()
    {
        _serializer ??= InitializeSerializer();
        
        return File.Exists(IndividualsJsonFileName)
            ? JsonConvert.DeserializeObject<Individual[]>(await File.ReadAllTextAsync(IndividualsJsonFileName, Encoding), _serializer)!
            : [];
    }

    public static async Task SaveRepository(IndividualRepository repository)
    {
        _serializer ??= InitializeSerializer();
        
        // FIXME: If the disk is full, this WILL corrupt the data that already exists, causing irrepairable loss.
        
        var serialized = JsonConvert.SerializeObject(repository.Individuals, Formatting.Indented, _serializer);
        await File.WriteAllTextAsync(IndividualsJsonFileName, serialized, Encoding);
    }
    
    public static string SerializeAsSingleLine(DataCollectionTrail trail)
    {
        _serializer ??= InitializeSerializer();
        
        return JsonConvert.SerializeObject(trail, Formatting.None, _serializer);
    }

    private static JsonSerializerSettings InitializeSerializer()
    {
        return new JsonSerializerSettings
        {
            Converters = { new StringEnumConverter() }
        };
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
                results.Add(JsonConvert.DeserializeObject<DataCollectionTrail>(line, _serializer));
            }
        }

        return results;
    }
}