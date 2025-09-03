using System.IO;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using XYVR.Core;
using XYVR.Data.Collection;
using XYVR.Scaffold;

namespace XYVR.UI.WebviewUI;

public partial class MainWindow : Window
{
    private const string VirtualHost = "appassets.example";
    
    private readonly AppBFF _appBff;
    private readonly DataCollectionBFF _dataCollectionBff;

    public IndividualRepository IndividualRepository { get; private set; }
    public ConnectorManagement ConnectorsMgt { get; private set; }
    public CredentialsManagement CredentialsMgt { get; private set; }

    public MainWindow()
    {
        InitializeComponent();
        _appBff = new AppBFF(this);
        _dataCollectionBff = new DataCollectionBFF(this);

        Title = "XYVR";
        
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs evt)
    {
        IndividualRepository = new IndividualRepository(await Scaffolding.OpenRepository());
        ConnectorsMgt = new ConnectorManagement(await Scaffolding.OpenConnectors());
        CredentialsMgt = new CredentialsManagement(await Scaffolding.OpenCredentials(), Scaffolding.ResoniteUIDLateInitializerFn());
        
        await WebView.EnsureCoreWebView2Async();
        WebView.CoreWebView2.AddHostObjectToScript("appApi", _appBff);
        WebView.CoreWebView2.AddHostObjectToScript("dataCollectionApi", _dataCollectionBff);

        // Intercept clicks on links
        WebView.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;

        var distPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "src/dist");
        WebView.CoreWebView2.SetVirtualHostNameToFolderMapping(VirtualHost, distPath, CoreWebView2HostResourceAccessKind.Allow);
        
        WebView.Source = new Uri($"https://{VirtualHost}/index.html");
    }

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
}