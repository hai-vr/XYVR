using System.Text.RegularExpressions;
using XYVR.AccountAuthority.Resonite;
using XYVR.AccountAuthority.VRChat;
using XYVR.Core;
using XYVR.Scaffold;

namespace XYVR.Program;

public enum Mode
{
    FetchTask,
    ManualTask,
    Duplicates,
}

internal partial class Program
{
    private static Mode mode = Mode.ManualTask;

    [GeneratedRegex(@"usr_[a-f0-9\-]+")]
    private static partial Regex UsrRegex();

    public static async Task Main()
    {
        var individuals = await Scaffolding.OpenRepository();
        
        var repository = new IndividualRepository(individuals);

        switch (mode)
        {
            case Mode.FetchTask:
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

                    await Scaffolding.SaveRepository(repository);
                }

                break;
            }
            case Mode.ManualTask:
            {
                var notNecessarilyValidUserIds = (await File.ReadAllLinesAsync("manual.txt"))
                    .Where(s => s.Contains("usr_"))
                    .Select(line =>
                    {
                        var match = UsrRegex().Match(line);
                        return match.Success ? match.Value : null;
                    })
                    .Where(result => result != null)
                    .ToList()!;

                var undiscoveredAccounts = await new VRChatCommunicator().CollectUndiscoveredLenient(repository, notNecessarilyValidUserIds);
            
                Console.WriteLine($"There are {undiscoveredAccounts.Count} undiscovered accounts in that manual file.");
                foreach (var undiscoveredAccount in undiscoveredAccounts)
                {
                    Console.WriteLine($"- {undiscoveredAccount.inAppDisplayName}");
                }
            
                if (undiscoveredAccounts.Count > 0)
                {
                    repository.MergeAccounts(undiscoveredAccounts);

                    await Scaffolding.SaveRepository(repository);
                }

                break;
            }
            case Mode.Duplicates:
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
                
                    await Scaffolding.SaveRepository(repository);
                }

                var individualsWithMultipleAccounts = repository.Individuals
                    .Where(individual => individual.accounts.Count >= 2)
                    .ToList();
            
                PrintIndividuals(individualsWithMultipleAccounts);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }

        // if (undiscoveredAccounts.Count > 0) PrintIndividuals(repository);
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