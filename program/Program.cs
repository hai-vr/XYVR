using System.Text.RegularExpressions;
using XYVR.AccountAuthority.VRChat;
using XYVR.Core;
using XYVR.Data.Collection;
using XYVR.Scaffold;

namespace XYVR.Program;

public enum Mode
{
    FetchIndividuals,
    ManualTask,
    Duplicates,
    ManualMerges,
    UpdateExistingIndividuals,
    RebuildFromStorage,
    UpdateAndGetNew
}

internal partial class Program
{
    private static Mode mode = Mode.UpdateAndGetNew;

    [GeneratedRegex(@"usr_[a-f0-9\-]+")]
    private static partial Regex UsrRegex();

    public static async Task Main()
    {
        var storage = new DataCollectionStorage();
        var individuals = await Scaffolding.OpenRepository();
        
        var repository = new IndividualRepository(individuals);

        switch (mode)
        {
            case Mode.RebuildFromStorage:
            {
                var trail = await Scaffolding.RebuildTrail();
                
                var dataCollection = new DataCollection(repository);
                
                foreach (var ind in repository.Individuals)
                {
                    foreach (var acc in ind.accounts)
                    {
                        acc.callers.Clear();
                    }
                }
                
                var rebuiltAccounts = await dataCollection.RebuildFromDataCollectionStorage(trail);
                if (rebuiltAccounts.Count > 0)
                {
                    repository.MergeAccounts(rebuiltAccounts);

                    await Scaffolding.SaveRepository(repository);
                }
                
                break;
            }
            case Mode.FetchIndividuals:
            {
                var dataCollection = new DataCollection(repository);

                var undiscoveredAccounts = await dataCollection.CollectAllUndiscoveredAccounts();
                if (undiscoveredAccounts.Count > 0)
                {
                    repository.MergeAccounts(undiscoveredAccounts);

                    await Scaffolding.SaveRepository(repository);
                }

                break;
            }
            case Mode.UpdateExistingIndividuals:
            {
                var dataCollection = new DataCollection(repository);

                var undiscoveredAccounts = await dataCollection.CollectExistingAccounts();
                if (undiscoveredAccounts.Count > 0)
                {
                    repository.MergeAccounts(undiscoveredAccounts);

                    await Scaffolding.SaveRepository(repository);
                }

                break;
            }
            case Mode.UpdateAndGetNew:
            {
                var dataCollection = new DataCollection(repository);

                var undiscoveredAccounts2 = await dataCollection.CollectExistingAccounts();
                if (undiscoveredAccounts2.Count > 0)
                {
                    repository.MergeAccounts(undiscoveredAccounts2);

                    await Scaffolding.SaveRepository(repository);
                }

                var undiscoveredAccounts = await dataCollection.CollectAllUndiscoveredAccounts();
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
                        // FIXME: We need to support oldschool user ids. Use "OnPlayerJoined .* \(([a-zA-Z0-9]{10})\)" as a log catcher in addition.
                        var match = UsrRegex().Match(line);
                        return match.Success ? match.Value : null;
                    })
                    .Where(result => result != null)
                    .Distinct()
                    .ToList();

                var undiscoveredAccounts = await new VRChatCommunicator(storage).CollectUndiscoveredLenient(repository, notNecessarilyValidUserIds);
            
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
            case Mode.ManualMerges:
            {
                // repository.FusionIndividuals(IndividualByAnyAccountId("usr_505d0888-f9f1-4ba1-92d5-71ff09607837"), IndividualByAnyAccountId("U-Hai"));
                // IndividualByAnyAccountId("usr_505d0888-f9f1-4ba1-92d5-71ff09607837").accounts.Add(ClusterAccount("vr_hai", "Haï~"));

                Account ClusterAccount(string inAppIdentifier, string inAppDisplayName)
                {
                    return new Account
                    {
                        namedApp = NamedApp.Cluster,
                        qualifiedAppName = "cluster",
                        inAppIdentifier = inAppIdentifier,
                        inAppDisplayName = inAppDisplayName,
                        callers = [
                            new CallerAccount
                            {
                                isAnonymous = false,
                                inAppIdentifier = "vr_hai",
                                isContact = true,
                                note = new Note
                                {
                                    status = NoteState.NeverHad,
                                    text = null
                                }
                            }
                        ]
                    };
                }

                await Scaffolding.SaveRepository(repository);
                
                Individual IndividualByAnyAccountId(string id)
                {
                    return repository.Individuals.First(individual => individual.accounts.Any(account => account.inAppIdentifier == id));
                }
                Account AccountByAnyId(string id)
                {
                    foreach (var ind in repository.Individuals)
                    {
                        foreach (var indAccount in ind.accounts)
                        {
                            if (indAccount.inAppIdentifier == id) return indAccount;
                        }
                    }

                    return null;
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
            }
        }
    }
}