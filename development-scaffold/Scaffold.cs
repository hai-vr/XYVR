using Newtonsoft.Json;
using XYVR.Core;

namespace XYVR.Scaffold;

public static class Scaffolding
{
    private const string IndividualsJsonFileName = "individuals.json";

    public static async Task<Individual[]> OpenRepository()
    {
        return File.Exists(IndividualsJsonFileName)
            ? JsonConvert.DeserializeObject<Individual[]>(await File.ReadAllTextAsync(IndividualsJsonFileName))!
            : [];
    }

    public static async Task SaveRepository(IndividualRepository repository)
    {
        var serialized = JsonConvert.SerializeObject(repository.Individuals, Formatting.Indented);
        await File.WriteAllTextAsync(IndividualsJsonFileName, serialized);
    }
}