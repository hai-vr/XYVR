using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;

namespace XYVR.API.Resonite;

public class ResoniteSignalRClient
{
    private readonly string _hubUrl = "https://api.resonite.com/hub";
    private HubConnection? _connection;
    
    public event StatusUpdate? OnStatusUpdate;
    public delegate Task StatusUpdate(UserStatusUpdate statusUpdate);

    public event Func<Task>? OnReconnected;

    public async Task StartAsync(ResAuthenticationStorage authStorage__sensitive)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl(_hubUrl, options =>
            {
                options.Headers.Add("Authorization", $"res {authStorage__sensitive.userId}:{authStorage__sensitive.token}");
            })
            .Build();

        // _connection.On<object>("ReceiveSessionUpdate", OnReceiveSessionUpdate);
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

    private void OnReceiveSessionUpdate(object sessionUpdate)
    {
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