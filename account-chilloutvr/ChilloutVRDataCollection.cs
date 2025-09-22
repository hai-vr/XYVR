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

            var results = new List<ImmutableAccountIdentification>();
        
            var contacts = await _api.GetContacts();
            foreach (var contact in contacts.data)
            {
                var update = repository.MergeIncompleteAccounts([
                    new ImmutableIncompleteAccount
                    {
                        namedApp = NamedApp.ChilloutVR,
                        qualifiedAppName = ChilloutVRAuthority.QualifiedAppName,
                        inAppIdentifier = contact.id,
                        inAppDisplayName = contact.name,
                        callers = [
                            new ImmutableIncompleteCallerAccount
                            {
                                isAnonymous = false,
                                inAppIdentifier = _userId,
                                isContact = true,
                                note = null
                            }
                        ]
                    }
                ]);
                repository.MergeAccounts([
                    new ImmutableNonIndexedAccount
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
                    }
                ]);
                await jobHandler.NotifyAccountUpdated(update.ToList());
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
        throw new NotImplementedException();
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