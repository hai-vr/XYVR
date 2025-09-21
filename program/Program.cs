using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using XYVR.Core;
using XYVR.Data.Collection;
using XYVR.Scaffold;

namespace XYVR.Program;

public enum Mode
{
    RebuildFromStorage,
    MigrateAndSave,
    Incremental,
    Test
}

internal class Program
{
    private static Mode mode = Mode.Test;

    public static async Task Main(string[] args)
    {
        Scaffolding.DefineSavePathFromArgsOrUseDefault(args);
        Scaffolding.CreateDirectoriesPertainingToSavePath();

        var serializer = new JsonSerializerSettings()
        {
            Converters = { new StringEnumConverter() }
        };
        
        var storage = new ResponseCollectionStorage();

        var repository = new IndividualRepository(await Scaffolding.OpenRepository());
        var connectors = new ConnectorManagement(await Scaffolding.OpenConnectors());
        var credentials = new CredentialsManagement(await Scaffolding.OpenCredentials(), await IAuthorityScaffolder.FindAll());
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
                var live = await credentials.GetConnectedLiveMonitoringOrNull(firstResoniteConnector, liveStatusMonitoring);
                if (live == null) throw new InvalidOperationException("No live monitoring available");
                
                await live.StartMonitoring();
                var i = 0;
                while (i < int.MaxValue) // this is just a placeholder so the ide doesn't complain
                {
                    await Task.Delay(TimeSpan.FromSeconds(10));
                    i++;
                }
                await live.StopMonitoring();

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
    public Task NotifyAccountUpdated(List<ImmutableAccountIdentification> increment)
    {
        XYVRLogging.WriteLine(this, $"Updated the following {increment.Count} accounts: {string.Join(", ", increment)}");
        return Task.CompletedTask;
    }

    public Task<IncrementalEnumerationTracker> NewEnumerationTracker()
    {
        return Task.FromResult(new IncrementalEnumerationTracker());
    }

    public Task NotifyEnumeration(IncrementalEnumerationTracker tracker, int enumerationAccomplished, int enumerationTotalCount_canBeZero)
    {
        XYVRLogging.WriteLine(this, $"Progress: {enumerationAccomplished} / {enumerationTotalCount_canBeZero}");
        return Task.CompletedTask;
    }

    public Task NotifyProspective(IncrementalEnumerationTracker tracker)
    {
        return Task.CompletedTask;
    }
}