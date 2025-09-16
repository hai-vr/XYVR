using Newtonsoft.Json;
using XYVR.Core;

namespace XYVR.AccountAuthority.Resonite;

public class ResoniteDataCollection(IndividualRepository repository, IResponseCollector responseCollectionStorage, string resoniteUid, ICredentialsStorage credentialsStorage) : IDataCollection
{
    private readonly ResoniteCommunicator _resoniteCommunicator = new(responseCollectionStorage, false, resoniteUid, credentialsStorage);

    public async Task<List<ImmutableNonIndexedAccount>> RebuildFromDataCollectionStorage(List<ResponseCollectionTrail> trails)
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

    public async Task<List<ImmutableAccountIdentification>> IncrementalUpdateRepository(IIncrementalDataCollectionJobHandler jobHandler)
    {
        var eTracker = await jobHandler.NewEnumerationTracker();

        var resoniteCaller = await _resoniteCommunicator.CallerAccount();
        repository.MergeAccounts([resoniteCaller]);
        await jobHandler.NotifyAccountUpdated([resoniteCaller.AsIdentification()]);
        await jobHandler.NotifyProspective(eTracker);

        var incAccs = await _resoniteCommunicator.FindIncompleteAccounts().ToListAsync();
        var whichIncompleteUpdated = repository.MergeIncompleteAccounts(incAccs);
        if (whichIncompleteUpdated.Count > 0) await jobHandler.NotifyAccountUpdated(whichIncompleteUpdated.ToList());

        var incompleteAccountsIds = new List<ImmutableAccountIdentification>();
        foreach (var incompleteAccount in incAccs)
        {
            var account = await _resoniteCommunicator.GetUser(incompleteAccount.inAppIdentifier, incompleteAccount.callers[0].isContact ?? false);
            if (account != null)
            {
                incompleteAccountsIds.Add(account.AsIdentification());
                var whichUpdated = repository.MergeAccounts([account]);
                if (whichUpdated.Count > 0) await jobHandler.NotifyAccountUpdated(whichUpdated.ToList());
                await jobHandler.NotifyEnumeration(eTracker, incompleteAccountsIds.Count, incAccs.Count);
            }
        }
        
        return new List<ImmutableAccountIdentification> { resoniteCaller.AsIdentification() }
            .Concat(incompleteAccountsIds)
            .Distinct()
            .ToList();
    }

    public bool CanAttemptIncrementalUpdateOn(ImmutableAccountIdentification identification)
    {
        return identification.namedApp == NamedApp.Resonite;
    }

    public async Task<ImmutableNonIndexedAccount?> TryGetForIncrementalUpdate__Flawed__NonContactOnly(ImmutableAccountIdentification toTryUpdate)
    {
        // FIXME: By default, this will always consider the returned user to be a non-Contact, so it's quite flawed. The name of this data collection method was changed BECAUSE OF this flaw in this specific collector.
        var result = await _resoniteCommunicator.CollectAllLenient([toTryUpdate.inAppIdentifier], []);
        
        return result.Count == 0 ? null : result.First();
    }
}