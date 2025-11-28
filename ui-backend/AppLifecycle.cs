using XYVR.Core;
using XYVR.Data.Collection;
using XYVR.Scaffold;
using XYVR.UI.Backend.AuxiliaryRepositories;

namespace XYVR.UI.Backend;

public class AppLifecycle
{
    public AppBFF AppBff { get; private set; } = null!;
    public DataCollectionBFF DataCollectionBff { get; private set; } = null!;
    public PreferencesBFF PreferencesBff { get; private set; } = null!;
    public LiveBFF LiveBff { get; private set; } = null!;

    public IndividualRepository IndividualRepository { get; private set; } = null!;
    public ConnectorManagement ConnectorsMgt { get; private set; } = null!;
    public CredentialsManagement CredentialsMgt { get; private set; } = null!;
    public LiveStatusMonitoring LiveStatusMonitoring { get; private set; } = null!;
    public LiveMonitoringAgent LiveMonitoringAgent { get; private set; } = null!;
    
    public ProfileIllustrationRepository ProfileIllustrationRepository { get; private set; } = null!;

    private List<IAuthority> _authorities = null!;

    private readonly Action<Action> _dispatchFn;
    private readonly Func<Func<Task>, Task> _dispatchAsyncFn;
    private Func<EventToSendToReact, Task> _scriptRunnerFn = null!;
    private CancellationTokenSource CancellationTokenSource;

    public AppLifecycle(Action<Action> dispatchFn, Func<Func<Task>, Task> dispatchAsyncFn)
    {
        _dispatchFn = dispatchFn;
        _dispatchAsyncFn = dispatchAsyncFn;
    }

    public void WhenApplicationStarts(string[] args)
    {
        // Order matters
        Scaffolding.DefineSavePathFromArgsOrUseDefault(args);
        Scaffolding.CreateDirectoriesPertainingToSavePath();
        Scaffolding.OpenDatabase();

        try
        {
            var lockfile = new FileLock(Scaffolding.LockfileFilePath);
            lockfile.AcquireLock();
        }
        catch (InvalidOperationException e)
        {
            // Do not use XYVRLogging here, this error is special.
            var error = "XYVR is already running on this save folder. To prevent data corruption, only one instance of XYVR can run at a time per save folder.";
            Console.Error.WriteLine(error);
            
            throw new InvalidOperationException(error, e);
        }

        // Setting up the log file must be done after the lock has been acquired.
        XYVRLogging.SetupLogFile();
        XYVRLogging.WriteLine(this, "Application startup");
        XYVRLogging.WriteLine(this, $"Version is {VERSION.version}");
    }

    public async Task WhenWindowLoaded(Func<EventToSendToReact, Task> scriptRunnerFn)
    {
        _scriptRunnerFn = scriptRunnerFn;
        
        CancellationTokenSource = new();
        
        AppBff = new AppBFF(this);
        DataCollectionBff = new DataCollectionBFF(this);
        PreferencesBff = new PreferencesBFF(this);
        LiveBff = new LiveBFF(this);
        
        _authorities = await IAuthorityScaffolder.FindAll(CancellationTokenSource);
        
        IndividualRepository = new IndividualRepository(await Scaffolding.OpenRepository());
        ConnectorsMgt = new ConnectorManagement(await Scaffolding.OpenConnectors());
        CredentialsMgt = new CredentialsManagement(await Scaffolding.OpenCredentials(), _authorities);
        LiveStatusMonitoring = new LiveStatusMonitoring();
        LiveMonitoringAgent = new LiveMonitoringAgent(ConnectorsMgt, CredentialsMgt, LiveStatusMonitoring, CancellationTokenSource);
        
        ProfileIllustrationRepository = new ProfileIllustrationRepository(await Scaffolding.OpenProfileIllustrationStorage());

        _ = Task.Run(() => LiveBff.StartMonitoring()); // don't wait this;
    }

    public async Task WhenApplicationCloses()
    {
        Task.Run(async () =>
        {
            XYVRLogging.WriteLine(this, "Application is closing.");
            try
            {
                XYVRLogging.WriteLine(this, "Saving...");
                
                await PreferencesBff.OnClosed();
                XYVRLogging.WriteLine(this, "Executed PreferencesBff.OnClosed");
                
                // This will cause the live monitoring tokens to cancel.
                await LiveBff.OnClosed();
                XYVRLogging.WriteLine(this, "Executed LiveBff.OnClosed");
                
                foreach (var authority in _authorities)
                {
                    await authority.SaveWhateverNecessary();
                    XYVRLogging.WriteLine(this, $"Saved authority of type {authority.GetType().Name}");
                }
                
                XYVRLogging.WriteLine(this, "Cancelling token...");
                await CancellationTokenSource.CancelAsync();

                XYVRLogging.WriteLine(this, "WhenApplicationCloses has executed successfully");
            }
            catch (Exception exception)
            {
                XYVRLogging.ErrorWriteLine(this, $"WhenApplicationCloses raised an error: {exception.Message}");
                XYVRLogging.ErrorWriteLine(this, exception);
                throw;
            }
        }).Wait();
        Scaffolding.CloseDatabase();
        XYVRLogging.CleanupLogFile();
    }

    public void Dispatch(Action action)
    {
        _dispatchFn.Invoke(action);
    }

    public async Task DispatchAsync(Func<Task> action)
    {
        await _dispatchAsyncFn.Invoke(action);
    }

    internal async Task SendEventToReact(string eventType__vulnerableToInjections, object obj)
    {
        if (eventType__vulnerableToInjections.Contains('\'')) throw new ArgumentException("Event type cannot contain single quotes.");
    
        await _scriptRunnerFn(new EventToSendToReact(eventType__vulnerableToInjections, obj));
    }
}

public record EventToSendToReact(string eventType__vulnerableToInjections, object obj);