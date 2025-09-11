using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;
using XYVR.API.Audit;

namespace XYVR.API.Resonite;

internal class ResoniteSignalRClient
{
    private HubConnection? _connection;
    
    public event StatusUpdate? OnStatusUpdate;
    public delegate Task StatusUpdate(UserStatusUpdate statusUpdate);
    
    public event SessionUpdate? OnSessionUpdate;
    public delegate Task SessionUpdate(SessionUpdateJsonObject sessionUpdate);

    public event Func<Task>? OnReconnected;

    public async Task StartAsync(ResAuthenticationStorage authStorage__sensitive)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl(AuditUrls.ResoniteHubUrl, options =>
            {
                options.Headers.Add("Authorization", $"res {authStorage__sensitive.userId}:{authStorage__sensitive.token}");
            })
            .WithAutomaticReconnect()
            .Build();
        
        // FIXME: Enabling this causes our app to get blasted in the face with 50KB worth
        // of JSON data per second (0.4Mbps). This can't possibly be right
        // This traffic even happens when not subscribed to it. Let's not make it worse by 
        // having to deserialize its contents.
        // FIXME: Replace this with on-demand requests to the Resonite API whenever we have
        // a hash we can't resolve with the currently known session.
        _connection.On<object>("ReceiveSessionUpdate", OnReceiveSessionUpdate);
        
        _connection.On<object>("ReceiveStatusUpdate", OnReceiveStatusUpdate);
        
        _connection.Closed += WhenConnectionClosed;
        _connection.Reconnecting += WhenReconnecting;
        _connection.Reconnected += WhenReconnected;

        await _connection.StartAsync();
    }

    public async Task StopAsync()
    {
        if (_connection == null) return;
        
        await _connection.StopAsync();
        await _connection.DisposeAsync();
        _connection = null;
    }

    public async Task SubmitRequestStatus(string? userId = null, bool weAreInvisible = false)
    {
        EnsureConnected();

        await _connection!.SendAsync("RequestStatus", userId, weAreInvisible);
    }

    public async Task ListenOnContact(string userId)
    {
        EnsureConnected();

        await _connection!.SendAsync("ListenOnContact", userId);
    }

    public async Task ListenOnKey(string key)
    {
        EnsureConnected();

        await _connection!.SendAsync("ListenOnKey", key);
    }

    private void EnsureConnected()
    {
        if (_connection == null) throw new InvalidOperationException("Not connected");
    }

    private async Task OnReceiveStatusUpdate(object statusUpdate)
    {
        var rawText = ((JsonElement)statusUpdate).GetRawText();
        var obj = JsonConvert.DeserializeObject<UserStatusUpdate>(rawText);
        
        if (OnStatusUpdate != null) await OnStatusUpdate?.Invoke(obj);
    }

    private async Task OnReceiveSessionUpdate(object sessionUpdate)
    {
        var rawText = ((JsonElement)sessionUpdate).GetRawText();
        var obj = JsonConvert.DeserializeObject<SessionUpdateJsonObject>(rawText);

        if (OnSessionUpdate != null) await OnSessionUpdate?.Invoke(obj);
    }

    private Task WhenConnectionClosed(Exception? exception)
    {
        Console.WriteLine($"Connection closed. Exception?: {exception?.Message}");
        return Task.CompletedTask;
    }

    private Task WhenReconnecting(Exception? exception)
    {
        Console.WriteLine($"Reconnecting... Exception?: {exception?.Message}");
        return Task.CompletedTask;
    }

    private async Task WhenReconnected(string? connectionId)
    {
        Console.WriteLine($"Reconnected with connection ID: {connectionId}");
        if (OnReconnected != null) await OnReconnected?.Invoke();
    }
    
    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    public async ValueTask DisposeAsync()
    {
        if (_connection == null) return;
        
        await _connection.DisposeAsync();
    }
}