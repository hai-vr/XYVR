using Newtonsoft.Json;
using XYVR.API.Resonite;
using XYVR.Core;

namespace XYVR.Program;

internal class Program
{
    private const string IndividualsJsonFileName = "individuals.json";

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

        var repository = new IndividualRepository(
            JsonConvert.DeserializeObject<Individual[]>(await File.ReadAllTextAsync(IndividualsJsonFileName))!
        );
        
        var resoniteAccountIdentifiers = JsonConvert.DeserializeObject<Individual[]>(await File.ReadAllTextAsync(IndividualsJsonFileName))!
            .SelectMany(individual => individual.accounts)
            .Where(account => account.namedApp == NamedApp.Resonite)
            .Select(account => account.inAppIdentifier)
            .ToHashSet();

        var contacts = await api.GetUserContacts();
        
        var undiscoveredContacts = contacts.Where(contact => !resoniteAccountIdentifiers.Contains(contact.id)).ToList();
        var thereAreUndiscoveredContacts = undiscoveredContacts.Count > 0;
        if (thereAreUndiscoveredContacts)
        {
            Console.WriteLine($"Found {undiscoveredContacts.Count} undiscovered contacts:");
            foreach (var undiscoveredContact in undiscoveredContacts)
            {
                Console.WriteLine($"- {undiscoveredContact.contactUsername}");
            }
            
            var undiscoveredContactIdToUser = new Dictionary<string, CombinedContactAndUser>();
            foreach (var undiscoveredContact in undiscoveredContacts)
            {
                // Do this one by one. We don't want to abuse the Resonite API.
                var user = await api.GetUser(undiscoveredContact.id);
                undiscoveredContactIdToUser.Add(undiscoveredContact.id, new CombinedContactAndUser(undiscoveredContact.id, undiscoveredContact, user));
            }

            var undiscoveredAccounts = undiscoveredContactIdToUser.Values
                .Select(AsAccount)
                .ToList();
            repository.MergeAccounts(undiscoveredAccounts);
        }
        else
        {
            Console.WriteLine("There are no undiscovered contacts.");
        }
        
        var serialized = JsonConvert.SerializeObject(repository.Individuals, Formatting.Indented);
        await File.WriteAllTextAsync(IndividualsJsonFileName, serialized);

        if (thereAreUndiscoveredContacts)
        {
            foreach (var individual in repository.Individuals)
            {
                Console.WriteLine(individual.displayName);
                Console.WriteLine($"- displayName: {individual.displayName}");
                Console.WriteLine($"- isAnyContact: {individual.isAnyContact}");
                Console.WriteLine("- accounts:");
                foreach (var account in individual.accounts)
                {
                    Console.WriteLine($"  - account:");
                    Console.WriteLine($"    - qualifiedAppName: {account.qualifiedAppName}");
                    Console.WriteLine($"    - inAppDisplayName: {account.inAppDisplayName}");
                    Console.WriteLine($"    - inAppIdentifier: {account.inAppIdentifier}");
                    Console.WriteLine($"    - isContact: {account.isContact}");
                }
            }
        }
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

internal class CombinedContactAndUser(string contactId, ContactResponseElementJsonObject contact, UserResponseJsonObject user)
{
    public string ContactId { get; } = contactId;
    public ContactResponseElementJsonObject Contact { get; } = contact;
    public UserResponseJsonObject User { get; } = user;
}
