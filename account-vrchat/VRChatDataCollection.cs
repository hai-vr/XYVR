﻿using Newtonsoft.Json;
using XYVR.Core;

namespace XYVR.AccountAuthority.VRChat;

public class VRChatDataCollection(IndividualRepository repository, IResponseCollector responseCollectionStorage, ICredentialsStorage credentialsStorage) : IDataCollection
{
    private readonly VRChatCommunicator _vrChatCommunicator = new(
        responseCollectionStorage,
        credentialsStorage
    );
    
    public async Task<List<ImmutableNonIndexedAccount>> RebuildFromDataCollectionStorage(List<ResponseCollectionTrail> trails)
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

    public async Task<List<ImmutableAccountIdentification>> IncrementalUpdateRepository(IIncrementalDataCollectionJobHandler jobHandler)
    {
        var eTracker = await jobHandler.NewEnumerationTracker();
        
        var vrcCaller = await _vrChatCommunicator.CallerAccount();
        repository.MergeAccounts([vrcCaller]);
        await jobHandler.NotifyAccountUpdated([vrcCaller.AsIdentification()]);
        await jobHandler.NotifyProspective(eTracker);
        
        var undiscoveredUserIds = new HashSet<string>();
        var incompleteAccounts = new HashSet<ImmutableAccountIdentification>();
        await foreach (var incompleteAccount in _vrChatCommunicator.FindIncompleteAccountsMayIncludeDuplicateReferences())
        {
            undiscoveredUserIds.Add(incompleteAccount.inAppIdentifier);
            incompleteAccounts.Add(incompleteAccount.AsIdentification());
            
            var whichIncompleteUpdated = repository.MergeIncompleteAccounts([incompleteAccount]);
            if (whichIncompleteUpdated.Count > 0) await jobHandler.NotifyAccountUpdated(whichIncompleteUpdated.ToList());
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
            HashSet<ImmutableAccountIdentification> whichUpdated;
            if (collectUndiscoveredLenient.Count == 0)
            {
                var newlyLost = repository.MarkAccountLost(new ImmutableAccountIdentification
                {
                    qualifiedAppName = VRChatCommunicator.VRChatQualifiedAppName,
                    namedApp = NamedApp.VRChat,
                    inAppIdentifier = undiscoveredUserId
                });
                if (newlyLost != null)
                {
                    whichUpdated = [newlyLost];
                }
                else
                {
                    whichUpdated = [];
                }
            }
            else
            {
                whichUpdated = repository.MergeAccounts(collectUndiscoveredLenient);
            }
            
            if (whichUpdated.Count > 0) await jobHandler.NotifyAccountUpdated(whichUpdated.ToList());

            soFar++;
            await jobHandler.NotifyEnumeration(eTracker, soFar, incompleteAccounts.Count);
        }

        return new List<ImmutableAccountIdentification> { vrcCaller.AsIdentification() }
            .Concat(incompleteAccounts)
            .Distinct()
            .ToList();
    }

    public bool CanAttemptIncrementalUpdateOn(ImmutableAccountIdentification identification)
    {
        return identification.namedApp == NamedApp.VRChat;
    }

    public async Task<ImmutableNonIndexedAccount?> TryGetForIncrementalUpdate__Flawed__NonContactOnly(ImmutableAccountIdentification toTryUpdate)
    {
        if (toTryUpdate.namedApp != NamedApp.VRChat) throw new ArgumentException("Cannot attempt incremental update on non-VRChat account, it is the responsibility of the caller to invoke CanAttemptIncrementalUpdateOn beforehand");
        
        var collected = await _vrChatCommunicator.CollectAllLenient([toTryUpdate.inAppIdentifier]);
        
        return collected.Count == 0 ? null : collected.First();
    }
}