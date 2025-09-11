using System.IO;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using XYVR.Core;
using XYVR.Data.Collection;
using XYVR.Scaffold;

namespace XYVR.UI.WebviewUI;

public partial class MainWindow : Window
{
    private const string VirtualHost = "appassets.example";
    
    private readonly AppBFF _appBff;
    private readonly DataCollectionBFF _dataCollectionBff;
    private readonly PreferencesBFF _preferencesBff;
    private readonly LiveBFF _liveBff;
    private readonly JsonSerializerSettings _serializer;
    private WorldNameCache? _openWorldNameCache;

    public App AppHandle { get; private set; }
    public IndividualRepository IndividualRepository { get; private set; }
    public ConnectorManagement ConnectorsMgt { get; private set; }
    public CredentialsManagement CredentialsMgt { get; private set; }
    public LiveStatusMonitoring LiveStatusMonitoring { get; private set; }

    public MainWindow()
    {
        InitializeComponent();
        _appBff = new AppBFF(this);
        _dataCollectionBff = new DataCollectionBFF(this);
        _preferencesBff = new PreferencesBFF(this);
        _liveBff = new LiveBFF(this);
        _serializer = BFFUtils.NewSerializer();

        Title = XYVRValues.ApplicationTitle;
        
        Loaded += (sender, evt) => _ = MWL(sender, evt);
        Closed += async (sender, evt) => await OnClosed(sender, evt);
    }

    private async Task OnClosed(object? sender, EventArgs e)
    {
        try
        {
            _preferencesBff.OnClosed();
            _liveBff.OnClosed();
            if (_openWorldNameCache != null)
            {
                await Scaffolding.SaveWorldNameCache(_openWorldNameCache);
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            throw;
        }
    }

    private async Task MWL(object sender, RoutedEventArgs evt)
    {
        try
        {
            await MainWindow_Loaded();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private async Task MainWindow_Loaded()
    {
        Console.WriteLine("WebView: Main window loaded.");
        
        AppHandle = (App)Application.Current;

        _openWorldNameCache = await Scaffolding.OpenWorldNameCache();
        IndividualRepository = new IndividualRepository(await Scaffolding.OpenRepository());
        ConnectorsMgt = new ConnectorManagement(await Scaffolding.OpenConnectors());
        CredentialsMgt = new CredentialsManagement(await Scaffolding.OpenCredentials(), await IAuthorityScaffolder.FindAll());
        LiveStatusMonitoring = new LiveStatusMonitoring();

        _ = Task.Run(() => _liveBff.StartMonitoring()); // don't wait this;
        
        await WebView.EnsureCoreWebView2Async();
        WebView.CoreWebView2.AddHostObjectToScript("appApi", _appBff);
        WebView.CoreWebView2.AddHostObjectToScript("dataCollectionApi", _dataCollectionBff);
        WebView.CoreWebView2.AddHostObjectToScript("preferencesApi", _preferencesBff);
        WebView.CoreWebView2.AddHostObjectToScript("liveApi", _liveBff);

        // Intercept clicks on links
        WebView.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;
        WebView.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;

        var distPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "src/dist");
        WebView.CoreWebView2.SetVirtualHostNameToFolderMapping(VirtualHost, distPath, CoreWebView2HostResourceAccessKind.Allow);
        
        WebView.Source = new Uri($"https://{VirtualHost}/index.html");
    }

    // Triggered especially when the user middle-clicks a link.
    private void CoreWebView2_NewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs evt)
    {
        // Cancel the new window request to prevent popups from middle-clicks
        evt.Handled = true;
        
        var uriString = evt.Uri;
        var uri = new Uri(uriString);
        
        // If the middle click was on an external link, then open it in the default browser instead of a popup.
        if (uri.Host != VirtualHost)
        {
            if (uriString.ToLowerInvariant().StartsWith("https://") || uriString.ToLowerInvariant().StartsWith("http://"))
            {
                Scaffolding.DANGER_OpenUrl(uriString);
            }
        }
    }

    // Triggered when the user clicks a link.
    private void CoreWebView2_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs evt)
    {
        var uriString = evt.Uri;
        var uri = new Uri(uriString);
        
        if (uri.Host != VirtualHost)
        {
            evt.Cancel = true;
            if (uriString.ToLowerInvariant().StartsWith("https://") || uriString.ToLowerInvariant().StartsWith("http://"))
            {
                Scaffolding.DANGER_OpenUrl(uriString);
            }
        }
    }
    
    internal async Task SendEventToReact(string eventType__vulnerableToInjections, object obj)
    {
        if (eventType__vulnerableToInjections.Contains('\'')) throw new ArgumentException("Event type cannot contain single quotes.");
        
        var eventJson = JsonConvert.SerializeObject(obj, _serializer);
        var script = $"window.dispatchEvent(new CustomEvent('{eventType__vulnerableToInjections}', {{ detail: {eventJson} }}));";
        
        await Dispatcher.InvokeAsync(() =>
        {
            if (WebView?.CoreWebView2 != null)
            {
                WebView.CoreWebView2.ExecuteScriptAsync(script);
            }
        });
    }
}