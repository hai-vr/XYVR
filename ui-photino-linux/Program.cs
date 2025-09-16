using Photino.NET;
using System.Drawing;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using XYVR.UI.Backend;
using Photino.NET.Server;

namespace XYVR.UI.Photino
{
    class Program
    {
        private const string LoadBearingSpace = " "; // This space is load-bearing, it makes the icon work in Windows 11 ¯\_(ツ)_/¯
        
        private static JsonSerializerSettings _serializer = null!;
        private static PhotinoWindow _window = null!;
        private static AppLifecycle _appLifecycle = null!;

        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                _serializer = BFFUtils.NewSerializer();
            
                var appLifecycle = new AppLifecycle(DispatchFn);
                _appLifecycle = appLifecycle;
                _appLifecycle.WhenApplicationStarts(args);
            
                // Window title declared here for visibility
                var windowTitle = $"XYVR{LoadBearingSpace}";
            
                PhotinoServer
                    .CreateStaticFileServer(args, out string baseUrl)
                    .RunAsync();
            
                _window = new PhotinoWindow()
                    .SetTitle(windowTitle)
                    // Resize to a percentage of the main monitor work area
                    .SetUseOsDefaultSize(false)
                    .SetSize(new Size(600, 1000))
                    .Center()
                    // Users can resize windows by default.
                    // Let's make this one fixed instead.
                    // .SetResizable(false)
                    .RegisterWebMessageReceivedHandler((sender, message__sensitive) =>
                    {
                        if (sender == null) throw new InvalidOperationException("Got null sender");
                        
                        var window = (PhotinoWindow)sender;
                        Task.Run(async () => await HandleMessage(window, message__sensitive));
                    });

                _window.SetIconFile(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "favicon.ico" : "icon.png");

                _window.Load($"{baseUrl}/index.html");
                
                _appLifecycle.WhenWindowLoaded(SendEventToReact).Wait();
            
                // Starts the application event loop
                _window.WaitForClose();
            
                appLifecycle.WhenApplicationCloses().Wait();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
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

        private static async Task HandleMessage(PhotinoWindow window, string message__sensitive)
        {
            var sendMessage__sensitive = JsonConvert.DeserializeObject<PhotinoSendMessage>(message__sensitive, _serializer)!;
            var endpoint = GetEndpointOrNull(sendMessage__sensitive.payload.endpoint);
            
            var promiseId = sendMessage__sensitive.id;
            
            if (endpoint == null)
            {
                await ReplyError(window, promiseId, "Invalid endpoint");
                return;
            }

            try
            {
                var methodInfo = endpoint.GetType().GetMethod(sendMessage__sensitive.payload.methodName);
                if (methodInfo == null)
                {
                    await ReplyError(window, promiseId, $"Method '{sendMessage__sensitive.payload.methodName}' not found");
                    return;
                }

                var parameters__sensitive = sendMessage__sensitive.payload.parameters;
                var parameterTypes = methodInfo.GetParameters();

                var convertedParameters__sensitive = new object[parameters__sensitive.Length];
                for (var i = 0; i < parameters__sensitive.Length; i++)
                {
                    if (i < parameterTypes.Length)
                    {
                        var expectedType = parameterTypes[i].ParameterType;
                        if (parameters__sensitive[i] is JToken jToken)
                            convertedParameters__sensitive[i] = jToken.ToObject(expectedType)!;
                        else if (parameters__sensitive[i].GetType() != expectedType && expectedType != typeof(object))
                            convertedParameters__sensitive[i] = Convert.ChangeType(parameters__sensitive[i], expectedType);
                        else
                            convertedParameters__sensitive[i] = parameters__sensitive[i];
                    }
                    else
                    {
                        convertedParameters__sensitive[i] = parameters__sensitive[i];
                    }
                }

                var result = methodInfo.Invoke(endpoint, convertedParameters__sensitive);

                if (result is Task task)
                {
                    await task;

                    if (task.GetType().IsGenericType)
                    {
                        var property = task.GetType().GetProperty("Result");
                        var taskResult = property?.GetValue(task);
                        await ReplySuccess(window, promiseId, taskResult?.ToString() ?? null);
                    }
                    else
                    {
                        await ReplySuccess(window, promiseId, null);
                    }
                }
                else
                {
                    await ReplySuccess(window, promiseId, result?.ToString() ?? null);
                }
            }
            catch (Exception ex)
            {
                await ReplyError(window, promiseId, ex.Message);
            }
        }

        private static async Task ReplySuccess(PhotinoWindow window, string sendMessageId, string? result)
        {
            var receiveMessage = new PhotinoReceiveMessage
            {
                isPhotinoMessage = true,
                isEvent = false,
                id = sendMessageId,
                payload = result,
                isError = false
            };

            await window.SendWebMessageAsync(JsonConvert.SerializeObject(receiveMessage, _serializer));
        }


        private static async Task ReplyError(PhotinoWindow window, string promiseId, string invalidEndpoint)
        {
            var receiveMessage = new PhotinoReceiveMessage
            {
                isPhotinoMessage = true,
                isEvent = false,
                id = promiseId,
                payload = invalidEndpoint,
                isError = true
            };
            
            await window.SendWebMessageAsync(JsonConvert.SerializeObject(receiveMessage, _serializer));
        }

        private static object? GetEndpointOrNull(string payloadEndpoint)
        {
            return payloadEndpoint switch
            {
                "appApi" => _appLifecycle.AppBff,
                "dataCollectionApi" => _appLifecycle.DataCollectionBff,
                "preferencesApi" => _appLifecycle.PreferencesBff,
                "liveApi" => _appLifecycle.LiveBff,
                _ => null
            };
        }

        private static void DispatchFn(Action action)
        {
            action();
        }
    }
}
