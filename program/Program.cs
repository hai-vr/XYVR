using core;
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
    UpdateAndGetNew,
    SetupConnectors,
}

internal partial class Program
{
    private static Mode mode = Mode.FindNewIndividuals;

    public static async Task Main()
    {
        var storage = new DataCollectionStorage();

        var repository = new IndividualRepository(await Scaffolding.OpenRepository());
        var connectors = new ConnectorManagement(await Scaffolding.OpenConnectors());
        
        var dataCollection = new CompoundDataCollection([new ResoniteDataCollection(repository, storage), new VRChatDataCollection(repository, storage)]);

        switch (mode)
        {
            case Mode.SetupConnectors:
            {
                {
                    var resonite = connectors.CreateNewConnector();
                    
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
                    var vrchat = connectors.CreateNewConnector();
                    
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
                    var vrchat = connectors.CreateNewConnector();
                    
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
            case Mode.UpdateAndGetNew:
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
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}