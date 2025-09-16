using System.Net.WebSockets;
using System.Text;
using XYVR.API.Audit;
using XYVR.Core;

namespace XYVR.AccountAuthority.VRChat;

internal class VRChatWebsocketClient : IDisposable
{
    private readonly ClientWebSocket _webSocket;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public event Action<string>? MessageReceived;
    public event Action? Connected;
    public event Action<string>? Disconnected;
    public event Action<Exception>? Error;

    public bool IsConnected => _webSocket.State == WebSocketState.Open;
    
    private bool _disposed;

    public VRChatWebsocketClient()
    {
        _webSocket = new ClientWebSocket();
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public async Task Connect(string authToken)
    {
        try
        {
            var uri = new Uri($"{AuditUrls.VrcWebsocketUrl}?authToken={authToken}");
            _webSocket.Options.SetRequestHeader("User-Agent", XYVRValues.UserAgent);
            await _webSocket.ConnectAsync(uri, _cancellationTokenSource.Token);
            
            Connected?.Invoke();
            
            _ = Task.Run(async () => await ListenForMessages(_cancellationTokenSource.Token));
        }
        catch (Exception ex)
        {
            Error?.Invoke(ex);
            throw;
        }
    }

    private async Task ListenForMessages(CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];
        
        try
        {
            while (_webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                using var messageBuffer = new MemoryStream();
                WebSocketReceiveResult result;
                
                // Keep receiving until we get the complete message
                do
                {
                    result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                    
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", cancellationToken);
                        Disconnected?.Invoke("Connection closed by server");
                        return;
                    }
                    
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        messageBuffer.Write(buffer, 0, result.Count);
                    }
                } 
                while (!result.EndOfMessage);
                
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var messageBytes = messageBuffer.ToArray();
                    var message = Encoding.UTF8.GetString(messageBytes);
                    MessageReceived?.Invoke(message);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Error?.Invoke(ex);
            Disconnected?.Invoke($"Error: {ex.Message}");
        }
    }

    public async Task Disconnect()
    {
        if (_webSocket.State == WebSocketState.Open)
        {
            try
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnecting", 
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                Error?.Invoke(ex);
            }
        }
        
        await _cancellationTokenSource.CancelAsync();
        Disconnected?.Invoke("Client disconnected");
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _cancellationTokenSource.Cancel();
            _webSocket?.Dispose();
            _cancellationTokenSource?.Dispose();
            _disposed = true;
        }
    }
}