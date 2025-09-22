using System.Runtime.InteropServices;
using Newtonsoft.Json;
using XYVR.Core;
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
        XYVRLogging.WriteLine(this, "Getting preferences");
        _lastPrefs = await Scaffolding.OpenReactAppPreferences();
        
        return ToJson(_lastPrefs);
    }

    public Task SetPreferences(string preferences)
    {
        XYVRLogging.WriteLine(this, "Set preferences was called.");
        _newPrefs = JsonConvert.DeserializeObject<ReactAppPreferences>(preferences)!;
        return Task.CompletedTask;
    }

    public void OnClosed()
    {
        if (_newPrefs == null) return;
        if (_lastPrefs == null || !_newPrefs.Equals(_lastPrefs))
        {
            XYVRLogging.WriteLine(this, "Saving preferences");
            _lastPrefs = _newPrefs;
            Task.Run(async () =>
                {
                    try
                    {
                        await Scaffolding.SaveReactAppPreferences(_newPrefs);
                    }
                    catch (Exception e)
                    {
                        XYVRLogging.ErrorWriteLine(this, e);
                        throw;
                    }
                })
                .Wait();
        }
    }

    private string ToJson(ReactAppPreferences result)
    {
        return JsonConvert.SerializeObject(result, Formatting.None, _serializer);
    }
}
