using Newtonsoft.Json;
using XYVR.AccountAuthority.VRChat;
using XYVR.API.VRChat;
using XYVR.Core;

namespace XYVR.Data.Collection;

public class VRChatDataCollection(IndividualRepository repository, ResponseCollectionStorage responseCollectionStorage, ICredentialsStorage credentialsStorage) : IDataCollection
{
    private readonly VRChatCommunicator _vrChatCommunicator = new(
        responseCollectionStorage,
        credentialsStorage
    );
    
    public async Task<List<NonIndexedAccount>> RebuildFromDataCollectionStorage(List<ResponseCollectionTrail> trails)
    {
        await Task.CompletedTask;

        // WARNING: This currently supports only one caller account per platform.
        var successfulTrails = trails
            .Where(trail => trail.status == DataCollectionResponseStatus.Success)
            .GroupBy(trail => new { trail.route, trail.reason })
            .Select(grouping => grouping.Last())
            .ToList();
        
        var vrchatCallerTrail = successfulTrails.First(trail => trail.reason == DataCollectionReason.CollectCallerAccount && trail.apiSource == "vrchat_web_api");

        var vrchatCallerJson = JsonConvert.DeserializeObject<VRChatAuthUser>((string)vrchatCallerTrail.responseObject);

        var vrchatCallerInAppIdentifier = vrchatCallerJson.id;

        var vrchatAccounts = successfulTrails
            .Where(trail => (trail.reason == DataCollectionReason.CollectExistingAccount || trail.reason == DataCollectionReason.CollectUndiscoveredAccount)
                            && trail.apiSource == "vrchat_web_api"
                            && trail.route.StartsWith("https://api.vrchat.cloud/api/1/users/"))
            .Select(trail => JsonConvert.DeserializeObject<VRChatUser>((string)trail.responseObject))
            .Select(user => _vrChatCommunicator.ConvertUserAsAccount(user, vrchatCallerInAppIdentifier))
            .GroupBy(account => account.inAppIdentifier)
            .Select(accounts => accounts.Last())
            .ToList();

        return vrchatAccounts;
    }

    public async Task<List<AccountIdentification>> IncrementalUpdateRepository(IIncrementalDataCollectionJobHandler jobHandler)
    {
        var eTracker = await jobHandler.NewEnumerationTracker();
        
        var vrcCaller = await _vrChatCommunicator.CallerAccount();
        repository.MergeAccounts([vrcCaller]);
        await jobHandler.NotifyAccountUpdated([vrcCaller.AsIdentification()]);
        await jobHandler.NotifyProspective(eTracker);
        
        var undiscoveredUserIds = new HashSet<string>();
        var incompleteAccounts = new HashSet<AccountIdentification>();
        await foreach (var incompleteAccount in _vrChatCommunicator.FindIncompleteAccounts())
        {
            undiscoveredUserIds.Add(incompleteAccount.inAppIdentifier);
            incompleteAccounts.Add(incompleteAccount.AsIdentification());
            
            repository.MergeIncompleteAccounts([incompleteAccount]);
            await jobHandler.NotifyAccountUpdated([incompleteAccount.AsIdentification()]);
            await jobHandler.NotifyEnumeration(eTracker, 0, incompleteAccounts.Count);
        }

        // We prioritize accounts that are pending update
        var undiscoveredUserIdsPrioritized = repository.Individuals
            .SelectMany(individual => individual.accounts)
            .Where(account => account is { namedApp: NamedApp.VRChat, isPendingUpdate: true })
            .Select(account => account.inAppIdentifier)
            .Where(inAppIdentifier => undiscoveredUserIds.Contains(inAppIdentifier))
            .ToHashSet();
        undiscoveredUserIdsPrioritized.UnionWith(undiscoveredUserIds);

        var soFar = 0;
        foreach (var undiscoveredUserId in undiscoveredUserIdsPrioritized)
        {
            var collectUndiscoveredLenient = await _vrChatCommunicator.CollectAllLenient([undiscoveredUserId]);
            repository.MergeAccounts(collectUndiscoveredLenient);
            await jobHandler.NotifyAccountUpdated(collectUndiscoveredLenient.Select(account => account.AsIdentification()).ToList());

            soFar++;
            await jobHandler.NotifyEnumeration(eTracker, soFar, incompleteAccounts.Count);
        }

        return new List<AccountIdentification> { vrcCaller.AsIdentification() }
            .Concat(incompleteAccounts)
            .Distinct()
            .ToList();
    }

    public bool CanAttemptIncrementalUpdateOn(AccountIdentification identification)
    {
        return identification.namedApp == NamedApp.VRChat;
    }

    public async Task<NonIndexedAccount?> TryGetForIncrementalUpdate__Flawed__NonContactOnly(AccountIdentification toTryUpdate)
    {
        if (toTryUpdate.namedApp != NamedApp.VRChat) throw new ArgumentException("Cannot attempt incremental update on non-VRChat account, it is the responsibility of the caller to invoke CanAttemptIncrementalUpdateOn beforehand");
        
        var collected = await _vrChatCommunicator.CollectAllLenient([toTryUpdate.inAppIdentifier]);
        
        return collected.Count == 0 ? null : collected.First();
    }
}