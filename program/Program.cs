using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using XYVR.AccountAuthority.Resonite;
using XYVR.API.Resonite;
using XYVR.Core;
using XYVR.Data.Collection;
using XYVR.Scaffold;

namespace XYVR.Program;

public enum Mode
{
    RebuildFromStorage,
    MigrateAndSave,
    Incremental,
    SignalRTesting,
    Test
}

internal class Program
{
    private static Mode mode = Mode.Test;

    public static async Task Main(string[] args)
    {
        Scaffolding.DefineSavePathFromArgsOrUseDefault(args);

        var serializer = new JsonSerializerSettings()
        {
            Converters = { new StringEnumConverter() }
        };
        
        var storage = new ResponseCollectionStorage();

        var repository = new IndividualRepository(await Scaffolding.OpenRepository());
        var connectors = new ConnectorManagement(await Scaffolding.OpenConnectors());
        var credentials = new CredentialsManagement(await Scaffolding.OpenCredentials(), Scaffolding.ResoniteUIDLateInitializerFn());
        var liveStatusMonitoring = new LiveStatusMonitoring();

        var dataCollection = new CompoundDataCollection(repository, (await Task.WhenAll(connectors.Connectors
                .Where(connector => connector.refreshMode != RefreshMode.ManualUpdatesOnly)
                .Select(async connector => await credentials.GetConnectedDataCollectionOrNull(connector, repository, storage))
                .ToList()))
            .Where(collection => collection != null)
            .Cast<IDataCollection>()
            .ToList()) as IDataCollection;

        switch (mode)
        {
            case Mode.Test:
            {
                var firstResoniteConnector = connectors.Connectors.First(connector => connector.type == ConnectorType.ResoniteAPI);
                // var dc = await credentials.GetConnectedDataCollectionOrNull(connector, repository, storage) as ResoniteDataCollection;
                // var comm = dc.Temp__GetCommunicator();
                
                // var usr = await comm.GetUser(connector.account.inAppIdentifier, false);
                // Console.WriteLine(JsonConvert.SerializeObject(usr));
                var resoniteLiveUpdates = new ResoniteLiveUpdates(credentials.Temp__ExtractCredentials__sensitive(firstResoniteConnector.guid));
                resoniteLiveUpdates.OnLiveUpdateReceived += update =>
                {
                    Console.WriteLine($"OnLiveUpdateReceived: {JsonConvert.SerializeObject(update, serializer)}");
                    liveStatusMonitoring.Merge(update);
                    
                    return Task.CompletedTask;
                };
                await resoniteLiveUpdates.Connect();
                int i = 0;
                while (i < int.MaxValue) // this is just a placeholder so the ide doesn't complain
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    i++;
                }
                await resoniteLiveUpdates.Disconnect();

                break;
            }
            case Mode.SignalRTesting:
            {
                var firstResoniteConnector = connectors.Connectors.First(connector => connector.type == ConnectorType.ResoniteAPI);
                var resoniteDataCollector = await credentials.GetConnectedDataCollectionOrNull(firstResoniteConnector, repository, storage) as ResoniteDataCollection;

                var memoizerUserIdToUsername = new Dictionary<string, Account?>();
                
                var srClient = new ResoniteSignalRClient();
                srClient.OnStatusUpdate += async status =>
                {
                    if (!memoizerUserIdToUsername.TryGetValue(status.userId, out var account))
                    {
                        account = await resoniteDataCollector.Temp__GetCommunicator().GetUser(status.userId, false);
                        memoizerUserIdToUsername[status.userId] = account;
                    }

                    if (account != null)
                    {
                        Console.WriteLine($"{account.inAppDisplayName} is {status.onlineStatus} (in session {status.userSessionId})");
                    }
                };
                
                var extractCredentials__sensitive = credentials.Temp__ExtractCredentials__sensitive(firstResoniteConnector.guid);
                var resAuth__sensitive = JsonConvert.DeserializeObject<ResAuthenticationStorage>(await extractCredentials__sensitive.RequireCookieOrToken())!;
                await srClient.StartAsync(resAuth__sensitive);
                
                await Task.Delay(TimeSpan.FromSeconds(1));
                await srClient.SubmitRequestStatus();
                await Task.Delay(TimeSpan.FromMinutes(100));
                
                await srClient.StopAsync();
                break;
            }
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