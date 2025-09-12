using System.Windows;
using XYVR.Core;
using XYVR.Scaffold;

namespace XYVR.UI.WebviewUI;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Order matters
        Scaffolding.DefineSavePathFromArgsOrUseDefault(e.Args);
        var lockfile = new FileLock(Scaffolding.LockfileFilePath);
        lockfile.AcquireLock();

        Console.WriteLine("Application startup");
        Console.WriteLine($"Version is {VERSION.version}");
    }
}
