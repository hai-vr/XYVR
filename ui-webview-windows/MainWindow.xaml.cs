using System.IO;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using XYVR.Core;
using XYVR.Scaffold;
using XYVR.UI.Backend;

namespace XYVR.UI.WebviewUI;

public partial class MainWindow : Window
{
    private const string VirtualHost = "appassets.example";
    
    public App AppHandle { get; private set; }

    private readonly JsonSerializerSettings _serializer;

    public MainWindow()
    {
        InitializeComponent();
        _serializer = BFFUtils.NewSerializer();

        Title = XYVRValues.ApplicationTitle;
        
        Loaded += (sender, evt) => _ = MWL(sender, evt);
        Closed += async (sender, evt) => await OnClosed(sender, evt);
    }

    private async Task OnClosed(object? sender, EventArgs e)
    {
        await AppHandle.Lifecycle.WhenApplicationCloses();
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
        await AppHandle.Lifecycle.WhenWindowLoaded(SendScriptToReact);
        
        await WebView.EnsureCoreWebView2Async();
        WebView.CoreWebView2.AddHostObjectToScript("appApi", AppHandle.Lifecycle.AppBff);
        WebView.CoreWebView2.AddHostObjectToScript("dataCollectionApi", AppHandle.Lifecycle.DataCollectionBff);
        WebView.CoreWebView2.AddHostObjectToScript("preferencesApi", AppHandle.Lifecycle.PreferencesBff);
        WebView.CoreWebView2.AddHostObjectToScript("liveApi", AppHandle.Lifecycle.LiveBff);

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

    private async Task SendScriptToReact(EventToSendToReact eventToSendToReact)
    {
        if (eventToSendToReact.eventType__vulnerableToInjections.Contains('\'')) throw new ArgumentException("Event type cannot contain single quotes.");
        
        var detailString = JsonConvert.SerializeObject(eventToSendToReact.obj, _serializer);
        var script = $"window.dispatchEvent(new CustomEvent('{eventToSendToReact.eventType__vulnerableToInjections}', {{ detail: {detailString} }}));";
        
        await Dispatcher.InvokeAsync(() =>
        {
            if (WebView?.CoreWebView2 != null)
            {
                WebView.CoreWebView2.ExecuteScriptAsync(script);
            }
        });
    }
}