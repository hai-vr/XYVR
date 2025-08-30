using Newtonsoft.Json;
using XYVR.AccountAuthority.Resonite;
using XYVR.API.Resonite;
using XYVR.Core;

namespace XYVR.Program;

internal class Program
{
    private const string IndividualsJsonFileName = "individuals.json";

    public static async Task Main()
    {
        var repository = new IndividualRepository(
            JsonConvert.DeserializeObject<Individual[]>(await File.ReadAllTextAsync(IndividualsJsonFileName))!
        );

        var undiscoveredAccounts = await new ResoniteCommunicator().FindUndiscoveredAccounts(repository);
        
        Console.WriteLine($"There are {undiscoveredAccounts.Count} undiscovered individuals.");
        foreach (var undiscoveredAccount in undiscoveredAccounts)
        {
            Console.WriteLine($"- {undiscoveredAccount.inAppDisplayName}");
        }

        if (undiscoveredAccounts.Count > 0)
        {
            repository.MergeAccounts(undiscoveredAccounts);
        
            var serialized = JsonConvert.SerializeObject(repository.Individuals, Formatting.Indented);
            await File.WriteAllTextAsync(IndividualsJsonFileName, serialized);
        }

        if (undiscoveredAccounts.Count > 0) PrintIndividuals(repository);
    }

    private static void PrintIndividuals(IndividualRepository repository)
    {
        foreach (var individual in repository.Individuals)
        {
            Console.WriteLine(individual.displayName);
            Console.WriteLine($"- displayName: {individual.displayName}");
            Console.WriteLine($"- isAnyContact: {individual.isAnyContact}");
            Console.WriteLine("- accounts:");
            foreach (var account in individual.accounts)
            {
                Console.WriteLine($"  - account:");
                Console.WriteLine($"    - qualifiedAppName: {account.qualifiedAppName}");
                Console.WriteLine($"    - inAppDisplayName: {account.inAppDisplayName}");
                Console.WriteLine($"    - inAppIdentifier: {account.inAppIdentifier}");
                Console.WriteLine($"    - isContact: {account.isContact}");
            }
        }
    }
}