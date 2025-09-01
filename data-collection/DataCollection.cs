using Newtonsoft.Json;
using XYVR.AccountAuthority.Resonite;
using XYVR.AccountAuthority.VRChat;
using XYVR.API.Resonite;
using XYVR.API.VRChat;
using XYVR.Core;

namespace XYVR.Data.Collection;

public class DataCollection(IndividualRepository repository)
{
    private static readonly DataCollectionStorage DataCollectionStorage = new();
    private readonly ResoniteCommunicator _resoniteCommunicator = new(DataCollectionStorage);
    private readonly VRChatCommunicator _vrChatCommunicator = new(DataCollectionStorage);

    /// Collects all Resonite accounts and VRChat accounts from upstream but only returns accounts that have not been discovered yet.<br/>
    /// This does not return information about existing accounts, even if those existing accounts had been modified upstream.<br/>
    /// Internally, this lists all current friends and notes but only retrieves and returns the full account information of IDs that aren't in the repository yet.<br/>
    /// <br/>
    /// This does not modify the repository.
    public async Task<List<Account>> CollectAllUndiscoveredAccounts()
    {
        var resoniteCaller = await _resoniteCommunicator.CallerAccount();
        var vrcCaller = await _vrChatCommunicator.CallerAccount();
        
        using var cts = new CancellationTokenSource();
        var resoniteTask = _resoniteCommunicator.FindUndiscoveredAccounts(repository);
        var vrcTask = _vrChatCommunicator.FindUndiscoveredIncompleteAccounts(repository);
        try
        {
            await resoniteTask;
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

        var undiscoveredResoniteAccounts = resoniteTask.Result;
        var undiscoveredVrcAccounts = await _vrChatCommunicator.CollectUndiscoveredLenient(repository, undiscoveredUserIds);

        var undiscoveredAccounts = new List<Account>();
        if (!repository.CollectAllInAppIdentifiers(NamedApp.Resonite).Contains(resoniteCaller.inAppIdentifier)) undiscoveredAccounts.Add(resoniteCaller);
        if (!repository.CollectAllInAppIdentifiers(NamedApp.VRChat).Contains(vrcCaller.inAppIdentifier)) undiscoveredAccounts.Add(vrcCaller);
        undiscoveredAccounts.AddRange(undiscoveredResoniteAccounts);
        undiscoveredAccounts.AddRange(undiscoveredVrcAccounts);

        return undiscoveredAccounts;
    }
    
    /// Collects the Resonite accounts and VRChat accounts that are in the repository.<br/>
    /// This does return new accounts, even some calls have implied the existence of new accounts.<br/>
    /// This can return fewer accounts than there actually are in the repository if some accounts were removed upstream.<br/>
    /// <br/>
    /// This does not modify the repository.
    public async Task<List<Account>> CollectExistingAccounts()
    {
        var resoniteContactIds = await _resoniteCommunicator.CollectContactUserIdsToCombineWithUsers();
        var resoniteAccounts = await _resoniteCommunicator.CollectAllLenient(repository.CollectAllInAppIdentifiers(NamedApp.Resonite).ToList(), resoniteContactIds);
        var vrcAccounts = await _vrChatCommunicator.CollectAllLenient(repository.CollectAllInAppIdentifiers(NamedApp.VRChat).ToList());

        var undiscoveredAccounts = new List<Account>();
        undiscoveredAccounts.AddRange(resoniteAccounts);
        undiscoveredAccounts.AddRange(vrcAccounts);

        return undiscoveredAccounts;
    }
    
    /// Using a data collection storage, try to rebuild account data.
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
        var vrchatCallerTrail = successfulTrails.First(trail => trail.reason == DataCollectionReason.CollectCallerAccount && trail.apiSource == "vrchat_web_api");

        var resoniteCallerJson = JsonConvert.DeserializeObject<UserResponseJsonObject>((string)resoniteCallerTrail.responseObject);
        var vrchatCallerJson = JsonConvert.DeserializeObject<VRChatAuthUser>((string)vrchatCallerTrail.responseObject);

        var resoniteCallerInAppIdentifier = resoniteCallerJson.id;
        var vrchatCallerInAppIdentifier = vrchatCallerJson.id;

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
        
        var vrchatAccounts = successfulTrails
            .Where(trail => (trail.reason == DataCollectionReason.CollectExistingAccount || trail.reason == DataCollectionReason.CollectUndiscoveredAccount)
                            && trail.apiSource == "vrchat_web_api"
                            && trail.route.StartsWith("https://api.vrchat.cloud/api/1/users/"))
            .Select(trail => JsonConvert.DeserializeObject<VRChatUser>((string)trail.responseObject))
            .Select(user => _vrChatCommunicator.ConvertUserAsAccount(user, vrchatCallerInAppIdentifier))
            .GroupBy(account => account.inAppIdentifier)
            .Select(accounts => accounts.Last())
            .ToList();

        return resoniteAccounts.Concat(vrchatAccounts).ToList();
    }
}