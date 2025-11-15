using System;
using System.Threading.Tasks;
using ComputerMonitoringClient.Networks;
using ComputerMonitoringClient.Utils;
using Microsoft.Extensions.Logging;

namespace ComputerMonitoringClient.Services
{
    /// <summary>
    /// Service xử lý các lệnh điều khiển từ xa từ server/desktop
    /// </summary>
    public class RemoteControlService
    {
        private static readonly Lazy<RemoteControlService> _instance =
            new(() => new RemoteControlService());
        public static RemoteControlService Instance => _instance.Value;

        private readonly ILogger<RemoteControlService> _logger = LoggerProvider.CreateLogger<RemoteControlService>();
        private bool _isInitialized;

        private RemoteControlService() { }

        /// <summary>
        /// Khởi tạo service và đăng ký event handlers
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
            {
                _logger.LogWarning("RemoteControlService already initialized");
                return;
            }

            try
            {
                // Đăng ký event handler cho yêu cầu chụp màn hình
                MonitoringHubClient.Instance.OnScreenshotRequested += HandleScreenshotRequest;

                _isInitialized = true;
                _logger.LogInformation("✅ RemoteControlService initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to initialize RemoteControlService");
                throw;
            }
        }

        /// <summary>
        /// Tắt service và hủy đăng ký event handlers
        /// </summary>
        public void Shutdown()
        {
            if (!_isInitialized)
            {
                _logger.LogWarning("RemoteControlService not initialized");
                return;
            }

            try
            {
                // Hủy đăng ký event handlers
                MonitoringHubClient.Instance.OnScreenshotRequested -= HandleScreenshotRequest;

                _isInitialized = false;
                _logger.LogInformation("🔌 RemoteControlService shutdown successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error during RemoteControlService shutdown");
            }
        }

        /// <summary>
        /// Xử lý yêu cầu chụp màn hình từ server
        /// </summary>
        private async void HandleScreenshotRequest(long attemptId)
        {
            try
            {
                _logger.LogInformation($"📸 Screenshot request received for attemptId: {attemptId}");

                // Validate attemptId
                var currentAttemptId = AppHttpSession.CurrentAttemptId;
                if (attemptId != currentAttemptId)
                {
                    _logger.LogWarning($"Ignoring screenshot request. Expected attemptId: {currentAttemptId}, Got: {attemptId}");
                    return;
                }

                // Kiểm tra connection
                if (!MonitoringHubClient.Instance.IsConnected)
                {
                    _logger.LogError("Cannot process screenshot request: Not connected to server");
                    return;
                }

                _logger.LogInformation("📷 Capturing screenshot...");

                // Chụp màn hình và upload
                var result = await ScreenshotService.Instance.CaptureAndUploadAsync(
                    attemptId,
                    captureAll: true); // Chụp tất cả màn hình

                if (result.Success)
                {
                    _logger.LogInformation($"✅ Screenshot uploaded successfully. ImageId: {result.ImageId}, URL: {result.ImageUrl}");

                    // Gửi kết quả về server qua SignalR
                    await MonitoringHubClient.Instance.SubmitScreenshotAsync(
                        attemptId,
                        result.ImageUrl,
                        result.ImageId);

                    _logger.LogInformation("📤 Screenshot submitted to server successfully");
                }
                else
                {
                    _logger.LogError($"❌ Screenshot capture/upload failed: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error handling screenshot request");
            }
        }

        /// <summary>
        /// Kiểm tra trạng thái service
        /// </summary>
        public bool IsInitialized => _isInitialized;
    }
}
