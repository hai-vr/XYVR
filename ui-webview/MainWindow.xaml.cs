using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using XYVR.Core;
using XYVR.Scaffold;

namespace XYVR.UI.WebviewUI;

public partial class MainWindow : Window
{
    private readonly AppApi _appApi;
    public IndividualRepository IndividualRepository { get; private set; }

    public MainWindow()
    {
        InitializeComponent();
        _appApi = new AppApi(this);

        Title = "XYVR";
        
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        IndividualRepository = new IndividualRepository(await Scaffolding.OpenRepository());
        
        await WebView.EnsureCoreWebView2Async();
        WebView.CoreWebView2.AddHostObjectToScript("appApi", _appApi);

        WebView.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;

        var distPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "src/dist");
        WebView.CoreWebView2.SetVirtualHostNameToFolderMapping("appassets.example", distPath, CoreWebView2HostResourceAccessKind.Allow);
        
        WebView.Source = new Uri($"https://appassets.example/index.html");
    }

    private void CoreWebView2_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
    {
        var uriString = e.Uri;
        var uri = new Uri(uriString);
        
        if (uri.Host != "appassets.example")
        {
            e.Cancel = true;
            if (uriString.ToLowerInvariant().StartsWith("https://") || uriString.ToLowerInvariant().StartsWith("http://"))
            {
                try
                {
                    Scaffolding.DANGER_OpenUrl(uriString);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to open link in browser: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }
    }
}