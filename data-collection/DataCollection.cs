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
    /// This does not update existing accounts, even if those existing accounts had been modified upstream.<br/>
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

        var undiscoveredResontieAccounts = resoniteTask.Result;
        var undiscoveredVrcAccounts = await _vrChatCommunicator.CollectUndiscoveredLenient(repository, undiscoveredUserIds);

        var undiscoveredAccounts = new List<Account>();
        if (!repository.CollectAllInAppIdentifiers(NamedApp.Resonite).Contains(resoniteCaller.inAppIdentifier)) undiscoveredAccounts.Add(resoniteCaller);
        if (!repository.CollectAllInAppIdentifiers(NamedApp.VRChat).Contains(vrcCaller.inAppIdentifier)) undiscoveredAccounts.Add(vrcCaller);
        undiscoveredAccounts.AddRange(undiscoveredResontieAccounts);
        undiscoveredAccounts.AddRange(undiscoveredVrcAccounts);

        return undiscoveredAccounts;
    }
}