using Newtonsoft.Json;
using XYVR.AccountAuthority.Resonite;
using XYVR.AccountAuthority.VRChat;
using XYVR.Core;

namespace XYVR.Program;

internal class Program
{
    private const string IndividualsJsonFileName = "individuals.json";
    
    private static bool Debug_FetchTask = false;

    public static async Task Main()
    {
        var individuals = await OpenRepository();
        
        var repository = new IndividualRepository(individuals);

        if (Debug_FetchTask)
        {
            using var cts = new CancellationTokenSource();
            var whenAll = Task.WhenAll(new[]
            {
                Task.Run(async () => await new ResoniteCommunicator().FindUndiscoveredAccounts(repository), cts.Token),
                Task.Run(async () => await new VRChatCommunicator().FindUndiscoveredAccounts(repository), cts.Token)
            });
            var undiscoveredAccounts = await Execute(whenAll, cts);

            Console.WriteLine($"There are {undiscoveredAccounts.Count} undiscovered accounts.");
            foreach (var undiscoveredAccount in undiscoveredAccounts)
            {
                Console.WriteLine($"- {undiscoveredAccount.inAppDisplayName}");
            }

            if (undiscoveredAccounts.Count > 0)
            {
                repository.MergeAccounts(undiscoveredAccounts);

                await SaveRepository(repository);
            }
        }
        else
        {
            var duplicatesOutput = new List<string>();
            
            var byDisplayName = repository.Individuals
                .GroupBy(individual => individual.displayName.ToLowerInvariant())
                .ToDictionary(grouping => grouping.Key, grouping => grouping.ToList());
            foreach (var individual in byDisplayName.Where(pair => pair.Value.Count >= 2))
            {
                var names = individual.Value.SelectMany(individual1 => individual1.accounts).Select(account => $"{account.namedApp}: {account.inAppIdentifier}").ToList();
                duplicatesOutput.Add($"{individual.Value.First().displayName}: {individual.Value.Count} accounts ({string.Join(", ", names)})");
            }
            duplicatesOutput.Sort();

            if (duplicatesOutput.Count > 0)
            {
                await File.WriteAllLinesAsync("duplicates.txt", duplicatesOutput);

                foreach (var individual in byDisplayName.Where(pair => pair.Value.Count >= 2))
                {
                    Console.WriteLine($"{individual.Value[1].guid} is being fusioned into {individual.Value[0].guid} because they have the same display name.");
                    repository.FusionIndividuals(individual.Value[0], individual.Value[1]);
                }
                
                await SaveRepository(repository);
            }

            var individualsWithMultipleAccounts = repository.Individuals
                .Where(individual => individual.accounts.Count >= 2)
                .ToList();
            
            PrintIndividuals(individualsWithMultipleAccounts);
        }

        // if (undiscoveredAccounts.Count > 0) PrintIndividuals(repository);
    }

    private static async Task<Individual[]> OpenRepository()
    {
        return File.Exists(IndividualsJsonFileName)
            ? JsonConvert.DeserializeObject<Individual[]>(await File.ReadAllTextAsync(IndividualsJsonFileName))!
            : [];
    }

    private static async Task SaveRepository(IndividualRepository repository)
    {
        var serialized = JsonConvert.SerializeObject(repository.Individuals, Formatting.Indented);
        await File.WriteAllTextAsync(IndividualsJsonFileName, serialized);
    }

    private static async Task<List<Account>> Execute(Task<List<Account>[]> tasks, CancellationTokenSource cts)
    {
        try
        {
            return (await tasks).SelectMany(list => list).ToList();
        }
        catch
        {
            await cts.CancelAsync(); // Cancel any remaining tasks
            throw;
        }
    }

    private static void PrintIndividuals(List<Individual> repositoryIndividuals)
    {
        foreach (var individual in repositoryIndividuals)
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