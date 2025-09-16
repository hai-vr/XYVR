using Photino.NET;
using System.Drawing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using XYVR.UI.Backend;
using Photino.NET.Server;

namespace HelloPhotinoApp
{
    //NOTE: To hide the console window, go to the project properties and change the Output Type to Windows Application.
    // Or edit the .csproj file and change the <OutputType> tag from "WinExe" to "Exe".
    class Program
    {
        private static JsonSerializerSettings _serializer;
        private static PhotinoWindow? _window;
        private static AppLifecycle _appLifecycle;

        [STAThread]
        static void Main(string[] args)
        {
            _serializer = BFFUtils.NewSerializer();
            
            var appLifecycle = new AppLifecycle(DispatchFn);
            _appLifecycle = appLifecycle;
            _appLifecycle.WhenApplicationStarts(args);
            
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
                    Task.Run(async () => await HandleMessage(window, message));

                    // The message argument is coming in from sendMessage.
                    // "window.external.sendMessage(message: string)"
                    // var response = $"Received message: \"{message}\"";

                    // Send a message back the to JavaScript event handler.
                    // "window.external.receiveMessage(callback: Function)"
                    // window.SendWebMessage(response);
                })
                .SetIconFile("favicon.ico")
                .Load($"{baseUrl}/index.html");

            _appLifecycle.WhenWindowLoaded(SendEventToReact).Wait();
            
            _window.WaitForClose(); // Starts the application event loop
            // await appLifecycle.WhenApplicationCloses();
        }

        private static async Task SendEventToReact(EventToSendToReact eventToSendToReact)
        {
            if (eventToSendToReact.eventType__vulnerableToInjections.Contains('\'')) throw new ArgumentException("Event type cannot contain single quotes.");
            
            var receiveMessage = new PhotinoReceiveMessage
            {
                isPhotinoMessage = true,
                isEvent = true,                
                id = eventToSendToReact.eventType__vulnerableToInjections,
                payload = JsonConvert.SerializeObject(eventToSendToReact.obj, _serializer), // Yes, this is doubly serialized. It's done this way because since all BFFs also return string, this makes things more consistent.
                isError = false
            };

            await _window.SendWebMessageAsync(JsonConvert.SerializeObject(receiveMessage, _serializer));
        }

        private static async Task HandleMessage(PhotinoWindow window, string message)
        {
            var sendMessage = JsonConvert.DeserializeObject<PhotinoSendMessage>(message, _serializer)!;
            var endpoint = GetEndpointOrNull(sendMessage);
            if (endpoint == null) await ReplyError(window, sendMessage.id, "Invalid endpoint");

            try
            {
                var methodInfo = endpoint.GetType().GetMethod(sendMessage.payload.methodName);
                if (methodInfo == null)
                {
                    await ReplyError(window, sendMessage.id, $"Method '{sendMessage.payload.methodName}' not found");
                    return;
                }

                var parameters = sendMessage.payload.parameters;
                var parameterTypes = methodInfo.GetParameters();

                var convertedParameters = new object[parameters.Length];
                for (var i = 0; i < parameters.Length; i++)
                {
                    if (i < parameterTypes.Length)
                    {
                        var expectedType = parameterTypes[i].ParameterType;
                        if (parameters[i] is JToken jToken)
                            convertedParameters[i] = jToken.ToObject(expectedType);
                        else if (parameters[i].GetType() != expectedType && expectedType != typeof(object))
                            convertedParameters[i] = Convert.ChangeType(parameters[i], expectedType);
                        else
                            convertedParameters[i] = parameters[i];
                    }
                    else
                    {
                        convertedParameters[i] = parameters[i];
                    }
                }

                var result = methodInfo.Invoke(endpoint, convertedParameters);

                if (result is Task task)
                {
                    await task;

                    if (task.GetType().IsGenericType)
                    {
                        var property = task.GetType().GetProperty("Result");
                        var taskResult = property?.GetValue(task);
                        await ReplySuccess(window, sendMessage.id, taskResult?.ToString() ?? null);
                    }
                    else
                    {
                        await ReplySuccess(window, sendMessage.id, null);
                    }
                }
                else
                {
                    await ReplySuccess(window, sendMessage.id, result?.ToString() ?? null);
                }
            }
            catch (Exception ex)
            {
                await ReplyError(window, sendMessage.id, ex.Message);
            }
        }

        private static async Task ReplySuccess(PhotinoWindow window, string sendMessageId, string? result)
        {
            var receiveMessage = new PhotinoReceiveMessage
            {
                isPhotinoMessage = true,
                id = sendMessageId,
                payload = result,
                isError = false
            };

            await window.SendWebMessageAsync(JsonConvert.SerializeObject(receiveMessage, _serializer));
        }


        private static async Task ReplyError(PhotinoWindow window, string sendMessageId, string invalidEndpoint)
        {
            var receiveMessage = new PhotinoReceiveMessage
            {
                isPhotinoMessage = true,
                id = sendMessageId,
                payload = invalidEndpoint,
                isError = true
            };
            
            await window.SendWebMessageAsync(JsonConvert.SerializeObject(receiveMessage, _serializer));
        }

        private static object? GetEndpointOrNull(PhotinoSendMessage? sendMessage)
        {
            switch (sendMessage.payload.endpoint)
            {
                case "appApi": return _appLifecycle.AppBff;
                case "dataCollectionApi": return _appLifecycle.DataCollectionBff;
                case "preferencesApi": return _appLifecycle.PreferencesBff;
                case "liveApi": return _appLifecycle.LiveBff;
            }

            return null;
        }

        private static void DispatchFn(Action action)
        {
            action();
        }
    }
}

internal class PhotinoSendMessage
{
    public string id;
    public PhotinoSendMessagePayload payload;
}

internal class PhotinoSendMessagePayload
{
    public string endpoint;
    public string methodName;
    public object[] parameters;
}

internal class PhotinoReceiveMessage
{
    public bool isPhotinoMessage;
    public bool isEvent;
    
    public string id;
    public string? payload;
    public bool isError;
}
