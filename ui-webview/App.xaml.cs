using System.Windows;

namespace XYVR.UI.WebviewUI;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        Console.WriteLine("Application startup");
    }
}
