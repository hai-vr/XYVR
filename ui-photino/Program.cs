using Photino.NET;
using System.Drawing;
using XYVR.UI.Backend;
using Photino.NET.Server;

namespace HelloPhotinoApp
{
    //NOTE: To hide the console window, go to the project properties and change the Output Type to Windows Application.
    // Or edit the .csproj file and change the <OutputType> tag from "WinExe" to "Exe".
    class Program
    {
        private static PhotinoWindow? _window;

        [STAThread]
        static void Main(string[] args)
        {
            var appLifecycle = new AppLifecycle(DispatchFn);
            appLifecycle.WhenApplicationStarts(args);
            
            // Window title declared here for visibility
            var windowTitle = "XYVR";
            
            PhotinoServer
                .CreateStaticFileServer(args, out string baseUrl)
                .RunAsync();
            
            // Creating a new PhotinoWindow instance with the fluent API
            _window = new PhotinoWindow()
                .SetTitle(windowTitle)
                // Resize to a percentage of the main monitor work area
                .SetUseOsDefaultSize(false)
                .SetSize(new Size(1000, 600))
                // Center window in the middle of the screen
                .Center()
                // Users can resize windows by default.
                // Let's make this one fixed instead.
                // .SetResizable(false)
                // .RegisterCustomSchemeHandler("app", (object sender, string scheme, string url, out string contentType) =>
                // {
                //     contentType = "text/javascript";
                //     return new MemoryStream(Encoding.UTF8.GetBytes(@"
                //         (() =>{
                //             window.setTimeout(() => {
                //                 alert(`🎉 Dynamically inserted JavaScript.`);
                //             }, 1000);
                //         })();
                //     "));
                // })
                // Most event handlers can be registered after the
                // PhotinoWindow was instantiated by calling a registration 
                // method like the following RegisterWebMessageReceivedHandler.
                // This could be added in the PhotinoWindowOptions if preferred.
                .RegisterWebMessageReceivedHandler((object sender, string message) =>
                {
                    var window = (PhotinoWindow)sender;

                    // The message argument is coming in from sendMessage.
                    // "window.external.sendMessage(message: string)"
                    var response = $"Received message: \"{message}\"";

                    // Send a message back the to JavaScript event handler.
                    // "window.external.receiveMessage(callback: Function)"
                    window.SendWebMessage(response);
                })
                .SetIconFile("favicon.ico")
                .Load($"{baseUrl}/index.html");

            appLifecycle.WhenWindowLoaded(async script =>
            {
                await _window.SendWebMessageAsync(script);
            }).Wait();
            
            _window.WaitForClose(); // Starts the application event loop
            // await appLifecycle.WhenApplicationCloses();
        }

        private static void DispatchFn(Action action)
        {
            action();
        }
    }
}
