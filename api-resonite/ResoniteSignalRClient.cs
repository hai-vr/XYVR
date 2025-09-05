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

        _connection.Closed += OnConnectionClosed;
        _connection.Reconnecting += OnReconnecting;
        _connection.Reconnected += OnReconnected;

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

    public async Task SubmitRequestStatus()
    {
        if (_connection == null) throw new InvalidOperationException("Not connected");
        
        await _connection.SendAsync("RequestStatus", null, false);
    }

    private async Task OnReceiveStatusUpdate(object statusUpdate)
    {
        var rawText = ((JsonElement)statusUpdate).GetRawText();
        var obj = JsonConvert.DeserializeObject<UserStatusUpdate>(rawText);
        Console.WriteLine($"Received status update: userId: {obj.userId} is now {obj.onlineStatus} in session {obj.userSessionId}");
        
        if (OnStatusUpdate != null) await OnStatusUpdate?.Invoke(obj);
    }

    private Task OnConnectionClosed(Exception? exception)
    {
        Console.WriteLine($"Connection closed. Exception?: {exception?.Message}");
        return Task.CompletedTask;
    }

    private Task OnReconnecting(Exception? exception)
    {
        Console.WriteLine($"Reconnecting... Exception?: {exception?.Message}");
        return Task.CompletedTask;
    }

    private Task OnReconnected(string? connectionId)
    {
        Console.WriteLine($"Reconnected with connection ID: {connectionId}");
        return Task.CompletedTask;
    }
    
    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    public async ValueTask DisposeAsync()
    {
        if (_connection == null) return;
        
        await _connection.DisposeAsync();
    }
}