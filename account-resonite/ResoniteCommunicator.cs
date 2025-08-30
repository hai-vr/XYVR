using XYVR.API.Resonite;
using XYVR.Core;

namespace XYVR.AccountAuthority.Resonite;

public class ResoniteCommunicator
{
    private readonly string _username__sensitive;
    private readonly string _password__sensitive;
    private readonly string _uid;

    public ResoniteCommunicator()
    {
        _username__sensitive = Environment.GetEnvironmentVariable(XYVREnvVar.ResoniteUsername)!;
        _password__sensitive = Environment.GetEnvironmentVariable(XYVREnvVar.ResonitePassword)!;
        _uid = Environment.GetEnvironmentVariable(XYVREnvVar.ResoniteUid)!;
        
        if (_username__sensitive == null || _password__sensitive == null) throw new ArgumentException("Missing environment variables");
        if (_uid == null)
        {
            Console.Error.WriteLine($"UID missing. Do you need one? Here's a random UID: {ResoniteAPI.RandomUID__NotCryptographicallySecure()}");
            throw new ArgumentException("Missing UID");
        }
    }
    
    public async Task<List<Account>> FindUndiscoveredAccounts(IndividualRepository individualRepository)
    {
        var resoniteAccountIdentifiers = individualRepository.CollectAllInAppIdentifiers(NamedApp.Resonite);
        
        var api = new ResoniteAPI(Guid.NewGuid().ToString(), _uid);
        
        await api.Login(_username__sensitive, _password__sensitive);

        var contacts = await api.GetUserContacts();
        
        var undiscoveredContacts = contacts.Where(contact => !resoniteAccountIdentifiers.Contains(contact.id)).ToList();

        if (undiscoveredContacts.Count == 0) return [];
        var undiscoveredContactIdToUser = new Dictionary<string, CombinedContactAndUser>();
        foreach (var undiscoveredContact in undiscoveredContacts)
        {
            // Do this one by one. We don't want to abuse the Resonite API.
            var user = await api.GetUser(undiscoveredContact.id);
            undiscoveredContactIdToUser.Add(undiscoveredContact.id, new CombinedContactAndUser(undiscoveredContact.id, undiscoveredContact, user));
        }

        return undiscoveredContactIdToUser.Values
            .Select(AsAccount)
            .ToList();
    }

    private static Account AsAccount(CombinedContactAndUser combined)
    {
        return new Account
        {
            namedApp = NamedApp.Resonite,
            qualifiedAppName = "resonite",
            inAppIdentifier = combined.User.id,
            inAppDisplayName = combined.User.username,
            liveServerData = combined,
            isContact = true,
        };
    }
}