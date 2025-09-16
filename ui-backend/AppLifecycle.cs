using Newtonsoft.Json;
using XYVR.Core;
using XYVR.Data.Collection;
using XYVR.Scaffold;

namespace XYVR.UI.Backend;

public class AppLifecycle
{
    public AppBFF AppBff { get; private set; }
    public DataCollectionBFF DataCollectionBff { get; private set; }
    public PreferencesBFF PreferencesBff { get; private set; }
    public LiveBFF LiveBff { get; private set; }

    public IndividualRepository IndividualRepository { get; private set; }
    public ConnectorManagement ConnectorsMgt { get; private set; }
    public CredentialsManagement CredentialsMgt { get; private set; }
    public LiveStatusMonitoring LiveStatusMonitoring { get; private set; }

    private List<IAuthority> _authorities;

    private readonly Action<Action> _dispatchFn;
    private Func<EventToSendToReact, Task> _scriptRunnerFn;

    public AppLifecycle(Action<Action> dispatchFn)
    {
        _dispatchFn = dispatchFn;
    }

    public void WhenApplicationStarts(string[] args)
    {
        // Order matters
        Scaffolding.DefineSavePathFromArgsOrUseDefault(args);
        Scaffolding.CreateDirectoriesPertainingToSavePath();
        
        var lockfile = new FileLock(Scaffolding.LockfileFilePath);
        lockfile.AcquireLock();

        Console.WriteLine("Application startup");
        Console.WriteLine($"Version is {VERSION.version}");
    }

    public async Task WhenWindowLoaded(Func<EventToSendToReact, Task> scriptRunnerFn)
    {
        _scriptRunnerFn = scriptRunnerFn;
        
        AppBff = new AppBFF(this);
        DataCollectionBff = new DataCollectionBFF(this);
        PreferencesBff = new PreferencesBFF(this);
        LiveBff = new LiveBFF(this);
        
        _authorities = await IAuthorityScaffolder.FindAll();
        
        IndividualRepository = new IndividualRepository(await Scaffolding.OpenRepository());
        ConnectorsMgt = new ConnectorManagement(await Scaffolding.OpenConnectors());
        CredentialsMgt = new CredentialsManagement(await Scaffolding.OpenCredentials(), _authorities);
        LiveStatusMonitoring = new LiveStatusMonitoring();

        _ = Task.Run(() => LiveBff.StartMonitoring()); // don't wait this;
    }

    public async Task WhenApplicationCloses()
    {
        try
        {
            Console.WriteLine("Saving...");
            PreferencesBff.OnClosed();
            LiveBff.OnClosed();
            foreach (var authority in _authorities)
            {
                await authority.SaveWhateverNecessary();
            }
            Console.WriteLine("Saved");
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            throw;
        }
    }

    public void Dispatch(Action action)
    {
        _dispatchFn.Invoke(action);
    }

    internal async Task SendEventToReact(string eventType__vulnerableToInjections, object obj)
    {
        if (eventType__vulnerableToInjections.Contains('\'')) throw new ArgumentException("Event type cannot contain single quotes.");
    
        await _scriptRunnerFn(new EventToSendToReact(eventType__vulnerableToInjections, obj));
    }
}

public record EventToSendToReact(string eventType__vulnerableToInjections, object obj);