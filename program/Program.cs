using Newtonsoft.Json;
using XYVR.API.Resonite;

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
        foreach (var contact in contacts)
        {
            Console.WriteLine(contact.contactUsername);
            var user = await api.GetUser(contact.id);
            Console.WriteLine($"- normalizedUsername: {user.normalizedUsername}");
            Console.WriteLine($"- isActiveSupporter: {user.isActiveSupporter}");
            Console.WriteLine($"- profile.iconUrl: {user.profile.iconUrl}");
            if (user.tags != null) Console.WriteLine($"- tags: {string.Join(",", user.tags)}");
        }
    }
}
