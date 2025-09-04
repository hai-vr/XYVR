using XYVR.Core;
using XYVR.Data.Collection;
using XYVR.Scaffold;

namespace XYVR.Program;

public enum Mode
{
    FindNewIndividuals,
    ManualMerges,
    UpdateExistingIndividuals,
    RebuildFromStorage,
    UpdateAllAndGetNew,
    UpdateOnlyThoseReturned,
    SetupConnectors,
    MigrateAndSave,
    Incremental,
}

internal partial class Program
{
    private static Mode mode = Mode.Incremental;

    public static async Task Main(string[] args)
    {
        Scaffolding.DefineSavePathFromArgsOrUseDefault(args);
        
        var storage = new ResponseCollectionStorage();

        var repository = new IndividualRepository(await Scaffolding.OpenRepository());
        var connectors = new ConnectorManagement(await Scaffolding.OpenConnectors());
        var credentials = new CredentialsManagement(await Scaffolding.OpenCredentials(), Scaffolding.ResoniteUIDLateInitializerFn());

        var dataCollection = new CompoundDataCollection(repository, (await Task.WhenAll(connectors.Connectors
                .Where(connector => connector.refreshMode != RefreshMode.ManualUpdatesOnly)
                .Select(async connector => await credentials.GetConnectedDataCollectionOrNull(connector, repository, storage))
                .ToList()))
            .Where(collection => collection != null)
            .Cast<IDataCollection>()
            .ToList()) as IDataCollection;

        switch (mode)
        {
            case Mode.MigrateAndSave:
            {
                await Scaffolding.SaveRepository(repository);
                
                break;
            }
            case Mode.Incremental:
            {
                await dataCollection.IncrementalUpdateRepository(new JobHandler());
                await Scaffolding.SaveRepository(repository);
                
                break;
            }
            case Mode.SetupConnectors:
            {
                {
                    var resonite = connectors.CreateNewConnector(ConnectorType.ResoniteAPI);
                    
                    resonite.account = new ConnectorAccount
                    {
                        namedApp = NamedApp.Resonite,
                        qualifiedAppName = "resonite",

                        inAppDisplayName = "Haï~",
                        inAppIdentifier = "U-Hai",
                    };
                    resonite.refreshMode = RefreshMode.ManualUpdatesOnly;
                    resonite.type = ConnectorType.ResoniteAPI;
                    
                    connectors.UpdateConnector(resonite);
                }
                {
                    var vrchat = connectors.CreateNewConnector(ConnectorType.VRChatAPI);
                    
                    vrchat.account = new ConnectorAccount
                    {
                        namedApp = NamedApp.VRChat,
                        qualifiedAppName = "vrchat",

                        inAppDisplayName = "Haï~",
                        inAppIdentifier = "usr_505d0888-f9f1-4ba1-92d5-71ff09607837",
                    };
                    vrchat.refreshMode = RefreshMode.ManualUpdatesOnly;
                    vrchat.type = ConnectorType.VRChatAPI;
                    
                    connectors.UpdateConnector(vrchat);
                }
                {
                    var vrchat = connectors.CreateNewConnector(ConnectorType.VRChatAPI);
                    
                    vrchat.account = new ConnectorAccount
                    {
                        namedApp = NamedApp.VRChat,
                        qualifiedAppName = "vrchat",

                        inAppDisplayName = "Baï~",
                        inAppIdentifier = "usr_3892108f-eb79-4120-980d-3fdc130f3b3b",
                    };
                    vrchat.refreshMode = RefreshMode.ManualUpdatesOnly;
                    vrchat.type = ConnectorType.VRChatAPI;
                    
                    connectors.UpdateConnector(vrchat);
                }
                
                await Scaffolding.SaveConnectors(connectors);

                break;
            }
            case Mode.RebuildFromStorage:
            {
                var trail = await Scaffolding.RebuildTrail();
                
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
            case Mode.FindNewIndividuals:
            {
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
                var undiscoveredAccounts = await dataCollection.CollectExistingAccounts();
                if (undiscoveredAccounts.Count > 0)
                {
                    repository.MergeAccounts(undiscoveredAccounts);

                    await Scaffolding.SaveRepository(repository);
                }

                break;
            }
            case Mode.UpdateAllAndGetNew:
            {
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
            case Mode.UpdateOnlyThoseReturned:
            {
                var returnedAccounts = await dataCollection.CollectReturnedAccounts();
                if (returnedAccounts.Count > 0)
                {
                    repository.MergeAccounts(returnedAccounts);

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
                        guid = Guid.NewGuid().ToString(),
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
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}

internal class JobHandler : IIncrementalDataCollectionJobHandler
{
    public Task NotifyAccountUpdated(List<AccountIdentification> increment)
    {
        Console.WriteLine($"Updated the following {increment.Count} accounts: {string.Join(", ", increment)}");
        return Task.CompletedTask;
    }

    public Task<IncrementalEnumerationTracker> NewEnumerationTracker()
    {
        return Task.FromResult(new IncrementalEnumerationTracker());
    }

    public Task NotifyEnumeration(IncrementalEnumerationTracker tracker, int enumerationAccomplished, int enumerationTotalCount_canBeZero)
    {
        Console.WriteLine($"Progress: {enumerationAccomplished} / {enumerationTotalCount_canBeZero}");
        return Task.CompletedTask;
    }

    public Task NotifyProspective(IncrementalEnumerationTracker tracker)
    {
        return Task.CompletedTask;
    }
}