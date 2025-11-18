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
        await AppHandle.Lifecycle.PreferencesBff.EditPrefs(preferences => preferences with
        {
            windowWidth = Width,
            windowHeight = Height,
            windowTop = Top,
            windowLeft = Left
        });
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
            XYVRLogging.ErrorWriteLine(this, e);
            throw;
        }
    }

    private async Task MainWindow_Loaded()
    {
        XYVRLogging.WriteLine(this, "WebView: Main window loaded.");
        
        AppHandle = (App)Application.Current;
        
        await AppHandle.Lifecycle.WhenWindowLoaded(SendScriptToReact);
        await AppHandle.Lifecycle.PreferencesBff.EditPrefs(preferences =>
        {
            Width = preferences.windowWidth;
            Height = preferences.windowHeight;
            Top = preferences.windowTop;
            Left = preferences.windowLeft;
            return preferences;
        });
        
        await WebView.EnsureCoreWebView2Async();
        WebView.CoreWebView2.AddHostObjectToScript("appApi", AppHandle.Lifecycle.AppBff);
        WebView.CoreWebView2.AddHostObjectToScript("dataCollectionApi", AppHandle.Lifecycle.DataCollectionBff);
        WebView.CoreWebView2.AddHostObjectToScript("preferencesApi", AppHandle.Lifecycle.PreferencesBff);
        WebView.CoreWebView2.AddHostObjectToScript("liveApi", AppHandle.Lifecycle.LiveBff);
        
        WebView.CoreWebView2.AddWebResourceRequestedFilter("thumbcache://*", CoreWebView2WebResourceContext.All);
        WebView.CoreWebView2.AddWebResourceRequestedFilter("individualprofile://*", CoreWebView2WebResourceContext.All);
        WebView.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;

        // Intercept clicks on links
        WebView.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;
        WebView.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;

        var distPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "src/dist");
        WebView.CoreWebView2.SetVirtualHostNameToFolderMapping(VirtualHost, distPath, CoreWebView2HostResourceAccessKind.Allow);
        
        WebView.Source = new Uri($"https://{VirtualHost}/index.html");
    }
    
    private void CoreWebView2_WebResourceRequested(object? sender, CoreWebView2WebResourceRequestedEventArgs e)
    {
        var requestUri = e.Request.Uri;
        var isAProfileRequest = requestUri.StartsWith("individualprofile://");
        
        if (!requestUri.StartsWith("thumbcache://") && !isAProfileRequest) return;

        var tail = isAProfileRequest ? requestUri.Substring("individualprofile://".Length) : requestUri.Substring("thumbcache://".Length);
        
        var lifecycleLiveBff = AppHandle.Lifecycle.LiveBff;
        var deferal = e.GetDeferral();

        Task.Run(async () => 
        {
            try
            {
                if (VRChatThumbnailCache.ContainsPathTraversalElements(tail))
                {
                    XYVRLogging.ErrorWriteLine(this, "URL suspiciously contains path traversal elements. We will return not found instead.");
                    
                    await Dispatcher.InvokeAsync(() => { e.Response = WebView.CoreWebView2.Environment.CreateWebResourceResponse(
                        null,
                        404,
                        "Not Found",
                        "Content-Type: text"
                    ); deferal.Complete(); });
                }
                else
                {
                    if (!isAProfileRequest)
                    {
                        var thumbnailHash = tail;
                        var thumbnailData = await lifecycleLiveBff.GetThumbnailBytesOrNull(thumbnailHash);
                        if (thumbnailData != null)
                        {
                            await Dispatcher.InvokeAsync(() => { e.Response = WebView.CoreWebView2.Environment.CreateWebResourceResponse(
                                new MemoryStream(thumbnailData),
                                200,
                                "OK",
                                "Content-Type: image/png"
                            ); deferal.Complete(); });
                        }
                        else
                        {
                            await Dispatcher.InvokeAsync(() => { e.Response = WebView.CoreWebView2.Environment.CreateWebResourceResponse(
                                null,
                                404,
                                "Not Found",
                                "Content-Type: text"
                            ); deferal.Complete(); });
                        }
                    }
                    else
                    {
                        var individualGuid = tail;
                        var profileIllustration = await AppHandle.Lifecycle.ProfileIllustrationRepository.GetOrNull(individualGuid);
                        if (profileIllustration != null)
                        {
                            await Dispatcher.InvokeAsync(() => { e.Response = WebView.CoreWebView2.Environment.CreateWebResourceResponse(
                                new MemoryStream(profileIllustration.data),
                                200,
                                "OK",
                                $"Content-Type: {profileIllustration.type}"
                            ); deferal.Complete(); });
                        }
                        else
                        {
                            await Dispatcher.InvokeAsync(() => { e.Response = WebView.CoreWebView2.Environment.CreateWebResourceResponse(
                                null,
                                404,
                                "Not Found",
                                "Content-Type: text"
                            ); deferal.Complete(); });
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                XYVRLogging.ErrorWriteLine(this, exception);
                throw;
            }
        });
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