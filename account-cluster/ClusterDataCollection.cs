using Newtonsoft.Json;
using XYVR.Core;

namespace XYVR.AccountAuthority.Cluster;

public class ClusterDataCollection : IDataCollection
{
    private readonly IndividualRepository _repository;
    private readonly ICredentialsStorage _credentialsStorage;
    private readonly IResponseCollector _storage;
    private readonly CancellationTokenSource _cancellationTokenSource;
    
    private ClusterAPI? _api;

    public ClusterDataCollection(IndividualRepository repository, ICredentialsStorage credentialsStorage, IResponseCollector storage, CancellationTokenSource cancellationTokenSource)
    {
        _repository = repository;
        _credentialsStorage = credentialsStorage;
        _storage = storage;
        _cancellationTokenSource = cancellationTokenSource;
    }

    public Task<List<ImmutableNonIndexedAccount>> RebuildFromDataCollectionStorage(List<ResponseCollectionTrail> trails)
    {
        throw new NotImplementedException();
    }

    public async Task<List<ImmutableAccountIdentification>> IncrementalUpdateRepository(IIncrementalDataCollectionJobHandler jobHandler)
    {
        _api ??= await InitializeApi();

        var caller = await _api.GetCallerAccount(DataCollectionReason.CollectCallerAccount);
        
        var friends = await _api.GetFriends(DataCollectionReason.FindUndiscoveredAccounts);
        var accounts = friends
            .Select(friend => ToAccount(friend, new ImmutableCallerAccount
            {
                isAnonymous = false,
                inAppIdentifier = caller.inAppIdentifier,
                note = new ImmutableNote
                {
                    status = NoteState.NeverHad,
                    text = null
                },
                isContact = true,
            }))
            .ToList();

        var updatedAccounts = new List<ImmutableAccountIdentification>();
        updatedAccounts.AddRange(_repository.MergeAccounts([caller]));
        updatedAccounts.AddRange(_repository.MergeAccounts(accounts));
        
        // TODO: Disjoint with friends from repository of this caller account, and set them isContact = false
        
        return updatedAccounts;
    }

    private ImmutableNonIndexedAccount ToAccount(ClusterUserInfo friend, ImmutableCallerAccount caller)
    {
        return new ImmutableNonIndexedAccount
        {
            namedApp = NamedApp.Cluster,
            qualifiedAppName = ClusterAuthority.QualifiedAppName,
            inAppIdentifier = friend.userId,
            inAppDisplayName = friend.displayName,
            specifics = new ImmutableClusterSpecifics
            {
                bio = friend.bio,
                username = friend.username
            },
            callers = [caller],
        };
    }

    private async Task<ClusterAPI> InitializeApi()
    {
        var api = new ClusterAPI(_storage, _cancellationTokenSource);
        var token__sensitive = await _credentialsStorage.RequireCookieOrToken();
        if (token__sensitive != null)
        {
            var deserialized__sensitive = JsonConvert.DeserializeObject<ClusterAuthStorage>(token__sensitive)!;
            api.Provide(deserialized__sensitive);
        }
        return api;
    }

    public bool CanAttemptIncrementalUpdateOn(ImmutableAccountIdentification identification)
    {
        throw new NotImplementedException();
    }

    public Task<ImmutableNonIndexedAccount?> TryGetForIncrementalUpdate__Flawed__NonContactOnly(ImmutableAccountIdentification toTryUpdate)
    {
        throw new NotImplementedException();
    }
}