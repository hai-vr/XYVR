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

        Console.Error.WriteLine($"UID missing. Do you need one? Here's a random UID: {ResoniteAPI.RandomUID__NotCryptographicallySecure()}");
        if (uid == null) throw new ArgumentException("Missing UID");
        
        Console.WriteLine($"uid is: {uid}");
        
        var api = new ResoniteAPI(Guid.NewGuid().ToString(), uid);
        var response = await api.CreateToken(username__sensitive, password__sensitive);
        
        var status = response.StatusCode;
        var content = await response.Content.ReadAsStringAsync();
        
        Console.WriteLine($"status is: {status}");
        Console.WriteLine(content);
    }
}
