using System.Windows;
using XYVR.UI.Backend;

namespace XYVR.UI.WebviewUI;

public partial class App : Application
{
    public AppLifecycle Lifecycle { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Lifecycle = new AppLifecycle(Current.Dispatcher.Invoke, async voidReturningFn => await Current.Dispatcher.InvokeAsync(voidReturningFn));
        Lifecycle.WhenApplicationStarts(e.Args);
    }
}
