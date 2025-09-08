using System.Windows;
using XYVR.Core;
using XYVR.Scaffold;

namespace XYVR.UI.WebviewUI;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Scaffolding.DefineSavePathFromArgsOrUseDefault(e.Args);

        Console.WriteLine("Application startup");
        Console.WriteLine($"Version is {VERSION.version}");
    }
}
