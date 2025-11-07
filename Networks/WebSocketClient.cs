using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace ComputerMonitoringClient.Networks
{
    namespace computer_monitoring_desktop.Networks
    {
        public sealed class WebSocketClient
        {
            private static readonly Lazy<WebSocketClient> _instance =
                new Lazy<WebSocketClient>(() => new WebSocketClient());

            public static WebSocketClient Instance => _instance.Value;

            private ClientWebSocket? _socket;
            private CancellationTokenSource? _cts;
            private bool _isReconnecting = false;

            public event Action? OnConnected;
            public event Action? OnDisconnected;
            public event Action<string>? OnMessage;
            public event Action<string>? OnError;

            private readonly string BASE_WS_URL =
                Environment.GetEnvironmentVariable("MoniTest_WS_URL") ??
                "ws://localhost:5045/ws/contests";

            private WebSocketClient() { }

            public async Task ConnectAsync(string? url = null)
            {
                var connectUrl = url ?? BASE_WS_URL;

                try
                {
                    _cts = new CancellationTokenSource();
                    _socket = new ClientWebSocket();

                    await _socket.ConnectAsync(new Uri(connectUrl), _cts.Token);
                    OnConnected?.Invoke();

                    _ = ReceiveLoopAsync(connectUrl); // chạy ngầm
                }
                catch (Exception ex)
                {
                    OnError?.Invoke(ex.Message);
                    await TryReconnectAsync(connectUrl);
                }
            }

            private async Task ReceiveLoopAsync(string url)
            {
                if (_socket == null) return;
                var buffer = new byte[4096];

                try
                {
                    while (_socket.State == WebSocketState.Open)
                    {
                        var result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), _cts!.Token);

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            OnDisconnected?.Invoke();
                            await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", CancellationToken.None);
                            break;
                        }

                        var msg = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        OnMessage?.Invoke(msg);
                    }
                }
                catch (Exception ex)
                {
                    OnError?.Invoke(ex.Message);
                    OnDisconnected?.Invoke();
                    await TryReconnectAsync(url);
                }
            }

            public async Task SendAsync(string message)
            {
                if (_socket == null || _socket.State != WebSocketState.Open)
                {
                    OnError?.Invoke("WebSocket not connected.");
                    return;
                }

                var bytes = Encoding.UTF8.GetBytes(message);
                var segment = new ArraySegment<byte>(bytes);
                await _socket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
            }

            public async Task DisconnectAsync()
            {
                try
                {
                    _cts?.Cancel();

                    if (_socket != null && _socket.State == WebSocketState.Open)
                    {
                        await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client closed", CancellationToken.None);
                    }

                    OnDisconnected?.Invoke();
                }
                catch (Exception ex)
                {
                    OnError?.Invoke(ex.Message);
                }
            }

            private async Task TryReconnectAsync(string url)
            {
                if (_isReconnecting) return;
                _isReconnecting = true;

                OnError?.Invoke("🔄 Mất kết nối, đang thử reconnect...");

                await Task.Delay(5000);
                _isReconnecting = false;

                await ConnectAsync(url);
            }
        }
    }

}
