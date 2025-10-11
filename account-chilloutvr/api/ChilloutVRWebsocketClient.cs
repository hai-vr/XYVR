﻿using System.Net.WebSockets;
using System.Text;
using XYVR.API.Audit;
using XYVR.Core;

namespace XYVR.AccountAuthority.ChilloutVR;

internal enum CvrWebsocketMessageType
{
    MENU_POPUP = 0,
    HUD_MESSAGE = 1,
    ONLINE_FRIENDS = 10,
    FRIEND_LIST_UPDATED = 11,
    INVITES = 15,
    REQUEST_INVITES = 20,
    FRIEND_REQUESTS = 25,
    MATURE_CONTENT_UPDATE = 30,
    GROUP_INVITE = 50,
}

internal class ChilloutVRWebsocketClient : IDisposable
{

    private readonly ClientWebSocket _webSocket;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public event Action<string>? MessageReceived;
    public event Action? Connected;
    public event Action<string>? Disconnected;
    public event Action<Exception>? Error;

    public bool IsConnected => _webSocket.State == WebSocketState.Open;

    private bool _disposed;

    public ChilloutVRWebsocketClient()
    {
        _webSocket = new ClientWebSocket();
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public async Task Connect(string user, string accessKey)
    {
        try
        {
            var uri = new Uri($"{AuditUrls.ChilloutVrWebsocketUrl}");
            _webSocket.Options.SetRequestHeader("Username", user);
            _webSocket.Options.SetRequestHeader("AccessKey", accessKey);
            _webSocket.Options.SetRequestHeader("User-Agent", XYVRValues.UserAgent);
            _webSocket.Options.SetRequestHeader("Platform", "pc_standalone");
            _webSocket.Options.SetRequestHeader("CompatibleVersions", "0,1,2");
            _webSocket.Options.SetRequestHeader("MatureContentDlc", "false");
            await _webSocket.ConnectAsync(uri, _cancellationTokenSource.Token);

            Connected?.Invoke();

            _ = Task.Run(async () =>
            {
                try
                {
                    await ListenForMessages(_cancellationTokenSource.Token);
                }
                catch (Exception e)
                {
                    XYVRLogging.ErrorWriteLine(this, e);
                    throw;
                }
            }, _cancellationTokenSource.Token);
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