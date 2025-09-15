using System.Windows;
using XYVR.UI.Backend;

namespace XYVR.UI.WebviewUI;

public partial class App : Application
{
    public AppLifecycle Lifecycle { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Lifecycle = new AppLifecycle(Current.Dispatcher.Invoke);
        Lifecycle.WhenApplicationStarts(e.Args);
    }
}
