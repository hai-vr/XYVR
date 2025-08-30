using XYVR.API.Resonite;
using XYVR.Core;

namespace XYVR.Program;

internal class Program
{
    public static async Task Main()
    {
        var username__sensitive = Environment.GetEnvironmentVariable("TEST_RESONITE_USERNAME");
        var password__sensitive = Environment.GetEnvironmentVariable("TEST_RESONITE_PASSWORD");
        var uid = Environment.GetEnvironmentVariable("TEST_RESONITE_UID");
        
        if (username__sensitive == null || password__sensitive == null) throw new ArgumentException("Missing environment variables");

        if (uid == null)
        {
            Console.Error.WriteLine($"UID missing. Do you need one? Here's a random UID: {ResoniteAPI.RandomUID__NotCryptographicallySecure()}");
            throw new ArgumentException("Missing UID");
        }
        
        Console.WriteLine($"uid is: {uid}");
        
        var api = new ResoniteAPI(Guid.NewGuid().ToString(), uid);
        await api.Login(username__sensitive, password__sensitive);

        var contacts = await api.GetUserContacts();
        var contactIdToUser = new Dictionary<string, CombinedContactAndUser>();
        foreach (var contact in contacts)
        {
            var user = await api.GetUser(contact.id);
            Console.WriteLine(contact.contactUsername);
            Console.WriteLine($"- id: {user.id}");
            Console.WriteLine($"- normalizedUsername: {user.normalizedUsername}");
            Console.WriteLine($"- isActiveSupporter: {user.isActiveSupporter}");
            Console.WriteLine($"- profile.iconUrl: {user.profile.iconUrl}");
            if (user.tags != null) Console.WriteLine($"- tags: {string.Join(",", user.tags)}");
            
            contactIdToUser.Add(contact.id, new CombinedContactAndUser(contact.id, contact, user));
        }

        var individuals = CreateIndividuals(contactIdToUser.Values.ToList());
    }

    private static List<Individual> CreateIndividuals(List<CombinedContactAndUser> combinedContacts)
    {
        var results = new List<Individual>();
        foreach (var combined in combinedContacts)
        {
            var individual = new Individual
            {
                guid = Guid.NewGuid().ToString(),
                accounts =
                [
                    new Account
                    {
                        namedApp = NamedApp.Resonite,
                        qualifiedAppName = "resonite",
                        inAppIdentifier = combined.User.id,
                        inAppDisplayName = combined.User.username,
                        liveServerData = combined,
                        isContact = true,
                    }
                ],
                displayName = combined.User.username
            };
            results.Add(individual);
        }

        return results;
    }
}

internal class CombinedContactAndUser(string contactId, ContactResponseElementJsonObject contact, UserResponseJsonObject user)
{
    public string ContactId { get; } = contactId;
    public ContactResponseElementJsonObject Contact { get; } = contact;
    public UserResponseJsonObject User { get; } = user;
}
