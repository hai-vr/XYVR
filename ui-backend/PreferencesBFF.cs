using System.Runtime.InteropServices;
using Newtonsoft.Json;
using XYVR.Scaffold;

namespace XYVR.UI.Backend;

[ComVisible(true)]
public interface IPreferencesBFF
{
    Task<string> GetPreferences();
    Task SetPreferences(string preferences);
}

[ComVisible(true)]
[ClassInterface(ClassInterfaceType.None)]
public class PreferencesBFF : IPreferencesBFF
{
    private readonly AppLifecycle _appLifecycle;
    private readonly JsonSerializerSettings _serializer;
    private ReactAppPreferences? _lastPrefs;
    private ReactAppPreferences? _newPrefs;

    public PreferencesBFF(AppLifecycle appLifecycle)
    {
        _appLifecycle = appLifecycle;
        _serializer = BFFUtils.NewSerializer();
    }
    
    public async Task<string> GetPreferences()
    {
        Console.WriteLine("Getting preferences");
        _lastPrefs = await Scaffolding.OpenReactAppPreferences();
        
        return ToJSON(_lastPrefs);
    }

    public async Task SetPreferences(string preferences)
    {
        Console.WriteLine("Set preferences was called.");
        _newPrefs = JsonConvert.DeserializeObject<ReactAppPreferences>(preferences)!;
    }

    public void OnClosed()
    {
        if (_newPrefs == null) return;
        if (_lastPrefs == null || !_newPrefs.Equals(_lastPrefs))
        {
            Console.WriteLine("Saving preferences");
            _lastPrefs = _newPrefs;
            Task.Run(async () => await Scaffolding.SaveReactAppPreferences(_newPrefs))
                .Wait();
        }
    }

    private string ToJSON(object result)
    {
        return JsonConvert.SerializeObject(result, Formatting.None, _serializer);
    }
}
