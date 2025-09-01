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
    FetchNotesUsingUserAPI,
    FetchNotesUsingNotesAPI,
}

internal partial class Program
{
    private static Mode mode = Mode.FetchIndividuals;

    [GeneratedRegex(@"usr_[a-f0-9\-]+")]
    private static partial Regex UsrRegex();

    public static async Task Main()
    {
        var storage = new DataCollectionStorage();
        var individuals = await Scaffolding.OpenRepository();
        
        var repository = new IndividualRepository(individuals);

        switch (mode)
        {
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
            case Mode.FetchNotesUsingUserAPI:
            {
                var individualsWithVRChatAccount = repository.Individuals
                    .Where(individual => individual.accounts.Any(account => account.namedApp == NamedApp.VRChat))
                    .ToList();
                
                var numberOfIndividualsUpdated = 0;

                var communicator = new VRChatCommunicator(storage);

                foreach (var individual in individualsWithVRChatAccount)
                {
                    var hasIndividualUpdated = false;
                    var vrchatAccountsOfThatIndividual = individual.accounts.Where(account => account.namedApp == NamedApp.VRChat).ToList();
                    foreach (var vrcAccount in vrchatAccountsOfThatIndividual)
                    {
                        var note = await communicator.TempCollectNoteFromUser(vrcAccount);
                        if (note is { } realNote)
                        {
                            if (realNote.status == NoteState.Exists)
                            {
                                if (vrcAccount.note.status != NoteState.Exists || vrcAccount.note.text != realNote.text)
                                {
                                    hasIndividualUpdated = true;
                                }
                                vrcAccount.note.status = NoteState.Exists;
                                vrcAccount.note.text = realNote.text;
                                
                                Console.WriteLine($"Note of {individual.displayName} is: {realNote.text}");
                            }
                            else
                            {
                                if (vrcAccount.note.status == NoteState.Exists)
                                {
                                    vrcAccount.note.status = NoteState.WasRemoved;
                                    hasIndividualUpdated = true;
                                }
                            }
                            
                        }
                        else
                        {
                            Console.WriteLine($"Trying to collect note of {individual.displayName} failed");
                        }
                    }

                    if (hasIndividualUpdated)
                    {
                        numberOfIndividualsUpdated++;
                    }
                }

                if (numberOfIndividualsUpdated > 0)
                {
                    Console.WriteLine($"Updated {numberOfIndividualsUpdated} individuals.");
                    
                    await Scaffolding.SaveRepository(repository);
                }

                break;
            }
            case Mode.FetchNotesUsingNotesAPI:
            {
                var notes = await new VRChatCommunicator(storage).TempGetNotes();
                foreach (var note in notes)
                {
                    Console.WriteLine($"{note.note} for {note.targetUserId} ({note.targetUser.displayName})");
                    if (AccountByAnyId(note.targetUserId, out var ind) is { } account)
                    {
                        if (!string.IsNullOrWhiteSpace(note.note))
                        {
                            account.note = new Note
                            {
                                status = NoteState.Exists,
                                text = note.note
                            };
                            ind.isExposed = true;
                        }
                    }
                }
                
                Account AccountByAnyId(string id, out Individual individual)
                {
                    foreach (var ind in repository.Individuals)
                    {
                        foreach (var indAccount in ind.accounts)
                        {
                            if (indAccount.inAppIdentifier == id)
                            {
                                individual = ind;
                                return indAccount;
                            }
                        }
                    }

                    individual = null;
                    return null;
                }

                var undiscoveredAccounts = await new VRChatCommunicator(storage).CollectUndiscoveredLenient(repository, notes.Select(full => full.targetUserId).Distinct().ToList());
            
                Console.WriteLine($"There are {undiscoveredAccounts.Count} undiscovered accounts in those notes.");
                foreach (var undiscoveredAccount in undiscoveredAccounts)
                {
                    Console.WriteLine($"- {undiscoveredAccount.inAppDisplayName} ({undiscoveredAccount.inAppIdentifier})");
                }
            
                if (undiscoveredAccounts.Count > 0)
                {
                    repository.MergeAccounts(undiscoveredAccounts);
                }

                await Scaffolding.SaveRepository(repository);

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
                        isContact = true,
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
                Console.WriteLine($"    - isContact: {account.isContact}");
            }
        }
    }
}