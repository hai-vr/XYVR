using Newtonsoft.Json;
using XYVR.AccountAuthority.Resonite;
using XYVR.API.Resonite;
using XYVR.Core;

namespace XYVR.Data.Collection;

public class ResoniteDataCollection(IndividualRepository repository, DataCollectionStorage dataCollectionStorage, string resoniteUid, ICredentialsStorage credentialsStorage) : IDataCollection
{
    private readonly ResoniteCommunicator _resoniteCommunicator = new(dataCollectionStorage,
        null, null, resoniteUid,
        credentialsStorage
    );

    public async Task<List<Account>> CollectAllUndiscoveredAccounts()
    {
        var resoniteCaller = await _resoniteCommunicator.CallerAccount();
        
        using var cts = new CancellationTokenSource();
        var resoniteTask = _resoniteCommunicator.FindUndiscoveredAccounts(repository);
        try
        {
            await resoniteTask;
        }
        catch (Exception _)
        {
            await cts.CancelAsync();
            throw;
        }

        var undiscoveredResoniteAccounts = resoniteTask.Result;

        var undiscoveredAccounts = new List<Account>();
        if (!repository.CollectAllInAppIdentifiers(NamedApp.Resonite).Contains(resoniteCaller.inAppIdentifier)) undiscoveredAccounts.Add(resoniteCaller);
        undiscoveredAccounts.AddRange(undiscoveredResoniteAccounts);

        return undiscoveredAccounts;
    }

    public async Task<List<Account>> CollectReturnedAccounts()
    {
        var resoniteCaller = await _resoniteCommunicator.CallerAccount();

        var incompleteAccounts = await _resoniteCommunicator.FindAccounts();

        return incompleteAccounts
            .Concat([resoniteCaller])
            .ToList();
    }
    
    public async Task<List<Account>> CollectExistingAccounts()
    {
        var resoniteContactIds = await _resoniteCommunicator.CollectContactUserIdsToCombineWithUsers();
        var resoniteAccounts = await _resoniteCommunicator.CollectAllLenient(repository.CollectAllInAppIdentifiers(NamedApp.Resonite).ToList(), resoniteContactIds);

        var undiscoveredAccounts = new List<Account>();
        undiscoveredAccounts.AddRange(resoniteAccounts);

        return undiscoveredAccounts;
    }
    
    public async Task<List<Account>> RebuildFromDataCollectionStorage(List<DataCollectionTrail> trails)
    {
        await Task.CompletedTask;

        // WARNING: This currently supports only one caller account per platform.
        var successfulTrails = trails
            .Where(trail => trail.status == DataCollectionResponseStatus.Success)
            .GroupBy(trail => new { trail.route, trail.reason })
            .Select(grouping => grouping.Last())
            .ToList();
        
        var resoniteCallerTrail = successfulTrails.First(trail => trail.reason == DataCollectionReason.CollectCallerAccount && trail.apiSource == "resonite_web_api");

        var resoniteCallerJson = JsonConvert.DeserializeObject<UserResponseJsonObject>((string)resoniteCallerTrail.responseObject);

        var resoniteCallerInAppIdentifier = resoniteCallerJson.id;

        var resoniteContactsTrail = successfulTrails
            .Last(trail => (trail.reason == DataCollectionReason.CollectExistingAccount || trail.reason == DataCollectionReason.CollectUndiscoveredAccount)
                           && trail.apiSource == "resonite_web_api"
                           && trail.route.StartsWith("https://api.resonite.com/users/")
                           && trail.route.EndsWith("/contacts")
                           && trail.route != "https://api.resonite.com/users/contacts"
            );
        var resoniteContactIds = JsonConvert.DeserializeObject<ContactResponseElementJsonObject[]>((string)resoniteContactsTrail.responseObject)
            .Select(o => o.id)
            .ToHashSet();

        var resoniteAccounts = successfulTrails
            .Where(trail => (trail.reason == DataCollectionReason.CollectExistingAccount || trail.reason == DataCollectionReason.CollectUndiscoveredAccount)
                            && trail.apiSource == "resonite_web_api"
                            && trail.route.StartsWith("https://api.resonite.com/users/")
                            && (!trail.route.EndsWith("/contacts") || trail.route == "https://api.resonite.com/users/contacts")
            )
            .Select(trail => JsonConvert.DeserializeObject<UserResponseJsonObject>((string)trail.responseObject))
            .Select(user => _resoniteCommunicator.ConvertUserAsAccount(user, resoniteCallerInAppIdentifier, resoniteContactIds))
            .GroupBy(account => account.inAppIdentifier)
            .Select(accounts => accounts.Last())
            .ToList();

        return resoniteAccounts;
    }

    public async Task<List<AccountIdentification>> IncrementalUpdateRepository(Func<List<AccountIdentification>, Task> incrementFn)
    {
        var resoniteCaller = await _resoniteCommunicator.CallerAccount();
        repository.MergeAccounts([resoniteCaller]);
        await incrementFn([resoniteCaller.AsIdentification()]);

        var incompleteAccounts = await _resoniteCommunicator.FindAccounts();
        repository.MergeAccounts(incompleteAccounts);
        await incrementFn(incompleteAccounts.Select(account => account.AsIdentification()).ToList());

        return new List<AccountIdentification> { resoniteCaller.AsIdentification() }
            .Concat(incompleteAccounts.Select(account => account.AsIdentification()))
            .Distinct()
            .ToList();
    }

    public bool CanAttemptIncrementalUpdateOn(AccountIdentification identification)
    {
        return identification.namedApp == NamedApp.Resonite;
    }

    public async Task<Account?> TryGetForIncrementalUpdate__Flawed__NonContactOnly(AccountIdentification toTryUpdate)
    {
        // FIXME: By default, this will always consider the returned user to be a non-Contact, so it's quite flawed. The name of this data collection method was changed BECAUSE OF this flaw in this specific collector.
        var result = await _resoniteCommunicator.CollectAllLenient([toTryUpdate.inAppIdentifier], []);
        
        return result.Count == 0 ? null : result.First();
    }
}