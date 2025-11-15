using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ComputerMonitoringClient.Utils;
using ComputerMonitoringClient.Dtos;
using ComputerMonitoringClient.Networks;

namespace ComputerMonitoringClient.Services
{
    public class MonitoringHubClient : IAsyncDisposable
    {
        private static readonly Lazy<MonitoringHubClient> _instance =
            new(() => new MonitoringHubClient());
        public static MonitoringHubClient Instance => _instance.Value;

        private HubConnection? _hubConnection;
        private readonly ILogger<MonitoringHubClient> _logger = LoggerProvider.CreateLogger<MonitoringHubClient>();

        public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

        public event Action<AttemptStatusUpdateDto>? OnAttemptStatusUpdated;
        public event Action<string>? OnStatusUpdated;
        public event Action? OnConnected;
        public event Action<Exception>? OnDisconnected;
        public event Action<string>? OnError;
        public event Action<long>? OnScreenshotRequested;

        private MonitoringHubClient() { }

        public async Task ConnectAsync(string token, int attemptId)
        {
            if (IsConnected && AppHttpSession.CurrentToken == token && AppHttpSession.CurrentAttemptId == attemptId)
                return;

            await DisconnectAsync();

            AppHttpSession.Token = token;
            AppHttpSession.CurrentToken = token;
            AppHttpSession.CurrentAttemptId = attemptId;

            var hubUrl = GetHubUrl();
            _logger.LogInformation("Connecting to SignalR hub: {Url}", hubUrl);

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult<string?>(AppHttpSession.CurrentToken);
                    options.Headers.Add("X-Client-Type", "Student");
                    options.HttpMessageHandlerFactory = handler =>
                    {
                        if (handler is System.Net.Http.HttpClientHandler httpHandler)
                            httpHandler.ServerCertificateCustomValidationCallback =
                                System.Net.Http.HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                        return handler;
                    };
                })
                .WithAutomaticReconnect(new[] {
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(5)
                })
                .ConfigureLogging(logging =>
                {
                    logging.SetMinimumLevel(LogLevel.Debug);
                    logging.AddConsole();
                })
                .Build();

            RegisterInternalEvents();
            RegisterHubEvents();

            try
            {
                await _hubConnection.StartAsync();
                _logger.LogInformation("✅ SignalR connected successfully");
                OnConnected?.Invoke();

                await Task.Delay(100);
                await JoinAttemptGroup(attemptId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to connect to SignalR hub");
                OnError?.Invoke($"Không thể kết nối SignalR: {ex.Message}");
                throw;
            }
        }

        private void RegisterInternalEvents()
        {
            if (_hubConnection == null) return;

            _hubConnection.Reconnecting += error =>
            {
                _logger.LogWarning("⚠️ SignalR reconnecting...");
                return Task.CompletedTask;
            };

            _hubConnection.Reconnected += connectionId =>
            {
                _logger.LogInformation("🔄 SignalR reconnected: {Id}", connectionId);
                OnConnected?.Invoke();
                return Task.CompletedTask;
            };

            _hubConnection.Closed += error =>
            {
                _logger.LogWarning("🔌 SignalR connection closed: {Msg}", error?.Message ?? "Normal");
                OnDisconnected?.Invoke(error ?? new Exception("Connection closed"));
                return Task.CompletedTask;
            };
        }

        private void RegisterHubEvents()
        {
            if (_hubConnection == null) return;

            _hubConnection.On<AttemptStatusUpdateDto>("AttemptStatusUpdated", dto =>
            {
                _logger.LogInformation("📩 Received status update: {Status}", dto.status);
                OnAttemptStatusUpdated?.Invoke(dto);
                OnStatusUpdated?.Invoke(dto.status);
            });

            _hubConnection.On<string>("Error", msg =>
            {
                _logger.LogError("❗ Received error from hub: {Error}", msg);
                OnError?.Invoke(msg);
            });

            // Nhận yêu cầu chụp màn hình từ server
            _hubConnection.On<object>("TakeScreenshot", async data =>
            {
                try
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(data);
                    var request = System.Text.Json.JsonDocument.Parse(json);
                    var attemptId = request.RootElement.GetProperty("attemptId").GetInt64();
                    
                    _logger.LogInformation("📸 Received screenshot request for attemptId: {AttemptId}", attemptId);
                    OnScreenshotRequested?.Invoke(attemptId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error handling TakeScreenshot event");
                }
            });
        }

        private async Task JoinAttemptGroup(int attemptId)
        {
            if (!IsConnected) return;
            try
            {
                await _hubConnection!.InvokeAsync("JoinAttemptGroup", attemptId);
                _logger.LogInformation("✅ Joined group for attemptId {AttemptId}", attemptId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to join attempt group");
            }
        }

        private async Task SendAsync(string method, params object[] args)
        {
            if (!IsConnected || _hubConnection == null) return;

            try
            {
                await _hubConnection.InvokeAsync(method, args);
                _logger.LogDebug("📤 Sent {Method} with {Count} args", method, args.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to send method {Method}", method);
            }
        }

        public async Task PingTest()
            => await _hubConnection.InvokeAsync("PingTest", "hello");

        public Task RegisterAttemptAsync(long attemptId, string deviceId, string deviceName, string ipAddress)
            => SendAsync("RegisterAttempt", attemptId, deviceId, deviceName, ipAddress);

        public Task SendMonitoringDataAsync(long attemptId, object monitoringData)
            => SendAsync("SendMonitoringData", attemptId, monitoringData);

        public async Task SendProcessListAsync(long attemptId, List<ProcessChangeDto> processes)
        {
            if (!IsConnected || _hubConnection == null) return;

            try
            {
                await _hubConnection.InvokeAsync("SendProcessList", attemptId, processes);
                _logger.LogDebug("📤 Sent SendProcessList with {Count} args", processes.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to send method SendProcessList");
            }
        }

        public Task SendTelemetryAsync(long attemptId, object telemetry)
            => SendAsync("SendTelemetry", attemptId, telemetry);

        public async Task SubmitScreenshotAsync(long attemptId, string imageUrl, long imageId)
        {
            if (!IsConnected || _hubConnection == null) return;

            try
            {
                await _hubConnection.InvokeAsync("SubmitScreenshot", attemptId, imageUrl, imageId);
                _logger.LogInformation("📤 Screenshot submitted: AttemptId={AttemptId}, ImageId={ImageId}", attemptId, imageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to submit screenshot");
            }
        }

        public async Task DisconnectAsync()
        {
            if (_hubConnection != null && _hubConnection.State != HubConnectionState.Disconnected)
            {
                try
                {
                    await _hubConnection.StopAsync();
                    _logger.LogInformation("🔕 Disconnected from SignalR hub");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while disconnecting SignalR");
                }
            }

            AppHttpSession.CurrentToken = null;
            AppHttpSession.CurrentAttemptId = null;
        }

        private static string GetHubUrl()
        {
            var baseUrl = Environment.GetEnvironmentVariable("MoniTest_BACKEND_URL") ?? "http://localhost:5045";
            if (baseUrl.EndsWith("/api/")) baseUrl = baseUrl[..^5];
            else if (baseUrl.EndsWith("/api")) baseUrl = baseUrl[..^4];
            return $"{baseUrl}/hubs/monitor";
        }

        public void ClearEventHandlers()
        {
            OnAttemptStatusUpdated = null;
            OnStatusUpdated = null;
            OnConnected = null;
            OnDisconnected = null;
            OnError = null;
            OnScreenshotRequested = null;
        }

        public async ValueTask DisposeAsync()
        {
            if (_hubConnection != null)
                await _hubConnection.DisposeAsync();
        }
    }
}
