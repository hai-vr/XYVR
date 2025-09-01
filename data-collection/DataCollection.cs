using XYVR.AccountAuthority.Resonite;
using XYVR.AccountAuthority.VRChat;
using XYVR.Core;

namespace XYVR.Data.Collection;

public class DataCollection(IndividualRepository repository)
{
    /// Collects all Resonite accounts and VRChat accounts from upstream but only returns accounts that have not been discovered yet.<br/>
    /// This does not update existing accounts, even if those existing accounts had been modified upstream.<br/>
    /// Internally, this lists all current friends and notes but only retrieves and returns the full account information of IDs that aren't in the repository yet.<br/>
    /// <br/>
    /// This does not modify the repository.
    public async Task<List<Account>> CollectAllUndiscoveredAccounts()
    {
        using var cts = new CancellationTokenSource();
        var resoniteTask = new ResoniteCommunicator().FindUndiscoveredAccounts(repository);
        var vrcTask = new VRChatCommunicator().FindUndiscoveredIncompleteAccounts(repository);
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
        
        var undiscoveredVrcAccounts = await new VRChatCommunicator().CollectUndiscoveredLenient(repository, undiscoveredUserIds);
        var undiscoveredAccounts = undiscoveredVrcAccounts.Concat(resoniteTask.Result).Distinct().ToList();

        return undiscoveredAccounts;
    }
}