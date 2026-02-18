using Newtonsoft.Json;
using XYVR.Core;

namespace XYVR.AccountAuthority.ChilloutVR;

public class ChilloutVRDataCollection(IndividualRepository repository, ICredentialsStorage credentialsStorage, IResponseCollector storage) : IDataCollection
{
    private ChilloutVRAPI? _api;
    private string? _userId;
    private string? _username;

    public Task<List<ImmutableNonIndexedAccount>> RebuildFromDataCollectionStorage(List<ResponseCollectionTrail> trails)
    {
        throw new NotImplementedException();
    }

    public async Task<List<ImmutableAccountIdentification>> IncrementalUpdateRepository(IIncrementalDataCollectionJobHandler jobHandler)
    {
        var eTracker = await jobHandler.NewEnumerationTracker(ChilloutVRAuthority.QualifiedAppName);

        try
        {
            _api ??= await InitializeAPI();
            if (_api == null || _userId == null || _username == null) throw new InvalidOperationException("Unable to initialize API");

            var local = repository.MergeAccounts([
                new ImmutableNonIndexedAccount
                {
                    namedApp = NamedApp.ChilloutVR,
                    qualifiedAppName = ChilloutVRAuthority.QualifiedAppName,
                    inAppIdentifier = _userId,
                    inAppDisplayName = _username,
                    callers = [
                        new ImmutableCallerAccount
                        {
                            isAnonymous = false,
                            inAppIdentifier = _userId,
                            isContact = true,
                            note = new ImmutableNote
                            {
                                status = NoteState.NeverHad,
                                text = null
                            }
                        }
                    ]
                }
            ]);
            await jobHandler.NotifyAccountUpdated(local.ToList());
            await jobHandler.NotifyProspective(eTracker);

            var results = new List<ImmutableAccountIdentification>();
        
            var contacts = await _api.GetContacts();
            foreach (var contact in contacts.data)
            {
                var account = new ImmutableNonIndexedAccount
                {
                    namedApp = NamedApp.ChilloutVR,
                    qualifiedAppName = ChilloutVRAuthority.QualifiedAppName,
                    inAppIdentifier = contact.id,
                    inAppDisplayName = contact.name,
                    callers =
                        [
                            new ImmutableCallerAccount
                            {
                                isAnonymous = false,
                                inAppIdentifier = _userId,
                                isContact = true,
                                note = new ImmutableNote
                                {
                                    status = NoteState.NeverHad,
                                    text = null
                                }
                            }
                        ]
                };
                results.Add(account.AsIdentification());
                var update = repository.MergeAccounts([account]);
                await jobHandler.NotifyAccountUpdated(update.ToList());
                await eTracker.Update(results.Count, contacts.data.Length);
            }

            return results;
        }
        catch (Exception e)
        {
            XYVRLogging.ErrorWriteLine(this, e);
            throw;
        }
    }

    public bool CanAttemptIncrementalUpdateOn(ImmutableAccountIdentification identification)
    {
        return identification.namedApp == NamedApp.ChilloutVR;
    }

    public Task<ImmutableNonIndexedAccount?> TryGetForIncrementalUpdate__Flawed__NonContactOnly(ImmutableAccountIdentification toTryUpdate)
    {
        if (toTryUpdate.namedApp != NamedApp.ChilloutVR) throw new ArgumentException("Cannot attempt incremental update on non-ChilloutVR account, it is the responsibility of the caller to invoke CanAttemptIncrementalUpdateOn beforehand");

        return Task.FromResult<ImmutableNonIndexedAccount?>(null);
    }
    

    private async Task<ChilloutVRAPI> InitializeAPI()
    {
        var api = new ChilloutVRAPI();
        var token__sensitive = await credentialsStorage.RequireCookieOrToken();
        if (token__sensitive != null)
        {
            var deserialized__sensitive = JsonConvert.DeserializeObject<ChilloutVRAuthStorage>(token__sensitive)!;
            _userId = deserialized__sensitive.userId;
            _username = deserialized__sensitive.username;
            api.Provide(deserialized__sensitive);
        }

        return api;
    }
}