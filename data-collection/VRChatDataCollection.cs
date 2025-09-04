using Newtonsoft.Json;
using XYVR.AccountAuthority.VRChat;
using XYVR.API.VRChat;
using XYVR.Core;

namespace XYVR.Data.Collection;

public class VRChatDataCollection(IndividualRepository repository, ResponseCollectionStorage responseCollectionStorage, ICredentialsStorage credentialsStorage) : IDataCollection
{
    private readonly VRChatCommunicator _vrChatCommunicator = new(
        responseCollectionStorage,
        null, null, null,
        credentialsStorage
    );

    public async Task<List<Account>> CollectAllUndiscoveredAccounts()
    {
        var vrcCaller = await _vrChatCommunicator.CallerAccount();
        
        using var cts = new CancellationTokenSource();
        var vrcTask = _vrChatCommunicator.FindUndiscoveredIncompleteAccounts(repository);
        try
        {
            await vrcTask;
        }
        catch (Exception _)
        {
            await cts.CancelAsync();
            throw;
        }

        var undiscoveredIncompleteAccounts = vrcTask.Result;
        var undiscoveredUserIds = undiscoveredIncompleteAccounts
            .Select(account => account.inAppIdentifier)
            .Distinct()
            .ToList();

        var undiscoveredVrcAccounts = await _vrChatCommunicator.CollectUndiscoveredLenient(repository, undiscoveredUserIds);

        var undiscoveredAccounts = new List<Account>();
        if (!repository.CollectAllInAppIdentifiers(NamedApp.VRChat).Contains(vrcCaller.inAppIdentifier)) undiscoveredAccounts.Add(vrcCaller);
        undiscoveredAccounts.AddRange(undiscoveredVrcAccounts);

        return undiscoveredAccounts;
    }

    public async Task<List<Account>> CollectReturnedAccounts()
    {
        var vrcCaller = await _vrChatCommunicator.CallerAccount();
        
        var incompleteAccounts = await _vrChatCommunicator.FindIncompleteAccounts();
        var undiscoveredUserIds = incompleteAccounts
            .Select(account => account.inAppIdentifier)
            .Distinct()
            .ToList();

        var collectUndiscoveredLenient = await _vrChatCommunicator.CollectAllLenient(undiscoveredUserIds);
        return collectUndiscoveredLenient
            .Concat([vrcCaller])
            .ToList();
    }
    
    public async Task<List<Account>> CollectExistingAccounts()
    {
        var vrcAccounts = await _vrChatCommunicator.CollectAllLenient(repository.CollectAllInAppIdentifiers(NamedApp.VRChat).ToList());

        var undiscoveredAccounts = new List<Account>();
        undiscoveredAccounts.AddRange(vrcAccounts);

        return undiscoveredAccounts;
    }
    
    public async Task<List<Account>> RebuildFromDataCollectionStorage(List<ResponseCollectionTrail> trails)
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

    public async Task<List<AccountIdentification>> IncrementalUpdateRepository(Func<List<AccountIdentification>, Task> incrementFn)
    {
        var vrcCaller = await _vrChatCommunicator.CallerAccount();
        repository.MergeAccounts([vrcCaller]);
        await incrementFn([vrcCaller.AsIdentification()]);
        
        var incompleteAccounts = await _vrChatCommunicator.FindIncompleteAccounts();
        repository.MergeIncompleteAccounts(incompleteAccounts);
        await incrementFn(incompleteAccounts.Select(account => account.AsIdentification()).ToList());
        
        var undiscoveredUserIds = incompleteAccounts
            .Select(account => account.inAppIdentifier)
            .Distinct()
            .ToList();
        
        foreach (var undiscoveredUserId in undiscoveredUserIds)
        {
            var collectUndiscoveredLenient = await _vrChatCommunicator.CollectAllLenient([undiscoveredUserId]);
            repository.MergeAccounts(collectUndiscoveredLenient);
            await incrementFn(collectUndiscoveredLenient.Select(account => account.AsIdentification()).ToList());
        }

        return new List<AccountIdentification> { vrcCaller.AsIdentification() }
            .Concat(incompleteAccounts.Select(account => account.AsIdentification()))
            .Distinct()
            .ToList();
    }

    public bool CanAttemptIncrementalUpdateOn(AccountIdentification identification)
    {
        return identification.namedApp == NamedApp.VRChat;
    }

    public async Task<Account?> TryGetForIncrementalUpdate__Flawed__NonContactOnly(AccountIdentification toTryUpdate)
    {
        if (toTryUpdate.namedApp != NamedApp.VRChat) throw new ArgumentException("Cannot attempt incremental update on non-VRChat account, it is the responsibility of the caller to invoke CanAttemptIncrementalUpdateOn beforehand");
        
        var collected = await _vrChatCommunicator.CollectAllLenient([toTryUpdate.inAppIdentifier]);
        
        return collected.Count == 0 ? null : collected.First();
    }
}