using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using ComputerMonitoringClient.Utils;
using ComputerMonitoringClient.Dtos;

namespace ComputerMonitoringClient.Services
{
    public class MonitoringHubClient : IAsyncDisposable
    {
        private static readonly Lazy<MonitoringHubClient> _instance =
            new Lazy<MonitoringHubClient>(() => new MonitoringHubClient());

        public static MonitoringHubClient Instance => _instance.Value;

        private HubConnection? _hubConnection;
        private readonly ILogger<MonitoringHubClient> _logger;
        private string? _currentToken;
        private int? _currentAttemptId;

        // Events for status updates
        public event Action<AttemptStatusUpdateDto>? OnAttemptStatusUpdated;
        public event Action<string>? OnStatusUpdated; // Backward compatible
        public event Action? OnConnected;
        public event Action<Exception>? OnDisconnected;
        public event Action<string>? OnError;

        // Public property to get current attemptId
        public int? CurrentAttemptId => _currentAttemptId;

        private MonitoringHubClient()
        {
            _logger = LoggerProvider.CreateLogger<MonitoringHubClient>();
        }

        /// <summary>
        /// Connect to SignalR hub with authentication token
        /// </summary>
        public async Task ConnectAsync(string token, int attemptId)
        {
            // If already connected to the same attempt, don't reconnect
            if (_hubConnection?.State == HubConnectionState.Connected && 
                _currentToken == token && 
                _currentAttemptId == attemptId)
            {
                _logger?.LogInformation("Already connected to attemptId: {AttemptId}", attemptId);
                return;
            }

            // If connected to different attempt, disconnect first
            if (_hubConnection?.State == HubConnectionState.Connected)
            {
                _logger?.LogInformation("Disconnecting from previous connection before reconnecting");
                await DisconnectAsync();
            }

            try
            {
                _currentToken = token;
                _currentAttemptId = attemptId;
                var hubUrl = GetHubUrl();

                _logger?.LogInformation("Connecting to SignalR hub: {HubUrl} for attemptId: {AttemptId}", hubUrl, attemptId);
                _logger?.LogDebug("Using token: {Token}", token?.Substring(0, Math.Min(20, token?.Length ?? 0)) + "...");

                _hubConnection = new HubConnectionBuilder()
                    .WithUrl(hubUrl, options =>
                    {
                        options.AccessTokenProvider = () => Task.FromResult<string?>(_currentToken);
                        // Add headers for additional debugging
                        options.Headers.Add("X-Client-Type", "Student");
                        // Skip certificate validation for development (remove in production)
                        options.HttpMessageHandlerFactory = (handler) =>
                        {
                            if (handler is System.Net.Http.HttpClientHandler httpHandler)
                            {
                                httpHandler.ServerCertificateCustomValidationCallback = 
                                    System.Net.Http.HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                            }
                            return handler;
                        };
                    })
                    .WithAutomaticReconnect(new[] { TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5) })
                    .ConfigureLogging(logging =>
                    {
                        logging.SetMinimumLevel(LogLevel.Debug);
                        logging.AddConsole();
                    })
                    .Build();

                // Register event handlers
                RegisterHandlers();

                // Handle reconnection
                _hubConnection.Reconnecting += error =>
                {
                    _logger?.LogWarning("SignalR connection lost. Reconnecting...");
                    return Task.CompletedTask;
                };

                _hubConnection.Reconnected += connectionId =>
                {
                    _logger?.LogInformation("SignalR reconnected. ConnectionId: {ConnectionId}", connectionId);
                    OnConnected?.Invoke();
                    return Task.CompletedTask;
                };

                _hubConnection.Closed += error =>
                {
                    if (error != null)
                    {
                        _logger?.LogError(error, "SignalR connection closed with error: {Message}", error.Message);
                    }
                    else
                    {
                        _logger?.LogInformation("SignalR connection closed normally");
                    }
                    OnDisconnected?.Invoke(error ?? new Exception("Connection closed"));
                    return Task.CompletedTask;
                };

                // Start connection
                await _hubConnection.StartAsync();
                
                _logger?.LogInformation("SignalR connected successfully. ConnectionId: {ConnectionId}", _hubConnection.ConnectionId);

                // Wait a bit for the connection to stabilize before joining group
                await Task.Delay(100);

                // Join monitoring group for this attempt
                try
                {
                    await JoinAttemptGroup(attemptId);
                }
                catch (Exception joinEx)
                {
                    _logger?.LogError(joinEx, "Failed to join attempt group during connection, will retry");
                    // Don't throw - connection is still valid, we can try to rejoin later
                }
                
                OnConnected?.Invoke();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to connect to SignalR hub");
                OnError?.Invoke($"Không thể kết nối SignalR: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Register SignalR message handlers
        /// </summary>
        private void RegisterHandlers()
        {
            if (_hubConnection == null) return;

            // Listen for attempt status updates from server
            // Server sends: { attemptId, status, attempt, timestamp }
            _hubConnection.On<AttemptStatusUpdateDto>("AttemptStatusUpdated", (updateDto) =>
            {
                _logger?.LogInformation(
                    "Received attempt {AttemptId} status update: {Status} at {Timestamp}", 
                    updateDto.attemptId, 
                    updateDto.status, 
                    updateDto.timestamp);

                // Fire the new detailed event
                OnAttemptStatusUpdated?.Invoke(updateDto);

                // Also fire the simple event for backward compatibility
                OnStatusUpdated?.Invoke(updateDto.status);
            });

            // Listen for errors
            _hubConnection.On<string>("Error", (errorMessage) =>
            {
                _logger?.LogError("Received error from hub: {Error}", errorMessage);
                OnError?.Invoke(errorMessage);
            });
        }

        /// <summary>
        /// Join the monitoring group for a specific attempt
        /// </summary>
        private async Task JoinAttemptGroup(int attemptId)
        {
            if (_hubConnection == null || _hubConnection.State != HubConnectionState.Connected)
            {
                _logger?.LogWarning("Cannot join group - hub not connected (State: {State})", _hubConnection?.State);
                return;
            }

            try
            {
                _logger?.LogInformation("Attempting to join monitoring group for attemptId: {AttemptId}", attemptId);
                await _hubConnection.InvokeAsync("JoinAttemptGroup", attemptId);
                _logger?.LogInformation("Successfully joined monitoring group for attemptId: {AttemptId}", attemptId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to join attempt group: {AttemptId}. Error: {Message}", attemptId, ex.Message);
                throw; // Re-throw to let caller handle
            }
        }

        /// <summary>
        /// Register attempt with device info
        /// </summary>
        public async Task RegisterAttemptAsync(long attemptId, string deviceId, string deviceName, string ipAddress)
        {
            if (_hubConnection == null || _hubConnection.State != HubConnectionState.Connected)
            {
                _logger?.LogWarning("Cannot register attempt - hub not connected");
                return;
            }

            try
            {
                await _hubConnection.InvokeAsync("RegisterAttempt", attemptId, deviceId, deviceName, ipAddress);
                _logger?.LogInformation("Registered attempt {AttemptId} with device {DeviceId}", attemptId, deviceId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to register attempt: {AttemptId}", attemptId);
            }
        }

        /// <summary>
        /// Send monitoring data to server
        /// </summary>
        public async Task SendMonitoringDataAsync(long attemptId, object monitoringData)
        {
            if (_hubConnection == null || _hubConnection.State != HubConnectionState.Connected)
            {
                _logger?.LogWarning("Cannot send monitoring data - hub not connected");
                return;
            }

            try
            {
                await _hubConnection.InvokeAsync("SendMonitoringData", attemptId, monitoringData);
                _logger?.LogDebug("Sent monitoring data for attempt {AttemptId}", attemptId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to send monitoring data for attempt: {AttemptId}", attemptId);
            }
        }

        /// <summary>
        /// Send process list to server
        /// </summary>
        public async Task SendProcessListAsync(long attemptId, List<object> processes)
        {
            if (_hubConnection == null || _hubConnection.State != HubConnectionState.Connected)
            {
                _logger?.LogWarning("Cannot send process list - hub not connected");
                return;
            }

            try
            {
                await _hubConnection.InvokeAsync("SendProcessList", attemptId, processes);
                _logger?.LogDebug("Sent process list for attempt {AttemptId}, Count: {Count}", attemptId, processes.Count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to send process list for attempt: {AttemptId}", attemptId);
            }
        }

        /// <summary>
        /// Send system telemetry to server
        /// </summary>
        public async Task SendTelemetryAsync(long attemptId, object telemetry)
        {
            if (_hubConnection == null || _hubConnection.State != HubConnectionState.Connected)
            {
                _logger?.LogWarning("Cannot send telemetry - hub not connected");
                return;
            }

            try
            {
                await _hubConnection.InvokeAsync("SendTelemetry", attemptId, telemetry);
                _logger?.LogDebug("Sent telemetry for attempt {AttemptId}", attemptId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to send telemetry for attempt: {AttemptId}", attemptId);
            }
        }

        /// <summary>
        /// Disconnect from SignalR hub
        /// </summary>
        public async Task DisconnectAsync()
        {
            if (_hubConnection != null && _hubConnection.State != HubConnectionState.Disconnected)
            {
                try
                {
                    _logger?.LogInformation("Disconnecting from SignalR hub");
                    await _hubConnection.StopAsync();
                    _logger?.LogInformation("SignalR disconnected");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error disconnecting from SignalR");
                }
            }

            _currentToken = null;
            _currentAttemptId = null;
        }

        /// <summary>
        /// Clear all event handlers (call when disposing forms)
        /// </summary>
        public void ClearEventHandlers()
        {
            OnAttemptStatusUpdated = null;
            OnStatusUpdated = null;
            OnConnected = null;
            OnDisconnected = null;
            OnError = null;
        }

        /// <summary>
        /// Get the SignalR hub URL from environment or default
        /// </summary>
        private string GetHubUrl()
        {
            var baseUrl = Environment.GetEnvironmentVariable("MoniTest_BACKEND_URL") ?? "http://localhost:5045";
            
            // Remove /api/ if present
            if (baseUrl.EndsWith("/api/"))
            {
                baseUrl = baseUrl.Substring(0, baseUrl.Length - 5);
            }
            else if (baseUrl.EndsWith("/api"))
            {
                baseUrl = baseUrl.Substring(0, baseUrl.Length - 4);
            }

            return $"{baseUrl}/hubs/monitor";
        }

        /// <summary>
        /// Check if connected
        /// </summary>
        public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

        public async ValueTask DisposeAsync()
        {
            if (_hubConnection != null)
            {
                await _hubConnection.DisposeAsync();
            }
        }
    }
}
