using XYVR.AccountAuthority.Resonite;
using XYVR.AccountAuthority.VRChat;
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
}