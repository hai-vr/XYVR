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

        var distPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "src/dist");
        WebView.CoreWebView2.SetVirtualHostNameToFolderMapping("appassets.example", distPath, CoreWebView2HostResourceAccessKind.Allow);
        
        WebView.Source = new Uri($"https://appassets.example/index.html");
    }
}