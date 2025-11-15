using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows.Forms;
using ComputerMonitoringClient.Networks;
using ComputerMonitoringClient.Utils;
using Microsoft.Extensions.Logging;

namespace ComputerMonitoringClient.Services
{
    public class ScreenshotService
    {
        private static readonly Lazy<ScreenshotService> _instance =
            new(() => new ScreenshotService());
        public static ScreenshotService Instance => _instance.Value;

        private readonly ILogger<ScreenshotService> _logger = LoggerProvider.CreateLogger<ScreenshotService>();
        private readonly HttpClient _httpClient;

        private ScreenshotService()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };
            _httpClient = new HttpClient(handler);
        }

        /// <summary>
        /// Chụp màn hình toàn bộ các màn hình
        /// </summary>
        public Bitmap CaptureAllScreens()
        {
            try
            {
                // Tính toán kích thước tổng của tất cả màn hình
                int minX = int.MaxValue;
                int minY = int.MaxValue;
                int maxX = int.MinValue;
                int maxY = int.MinValue;

                foreach (Screen screen in Screen.AllScreens)
                {
                    minX = Math.Min(minX, screen.Bounds.X);
                    minY = Math.Min(minY, screen.Bounds.Y);
                    maxX = Math.Max(maxX, screen.Bounds.Right);
                    maxY = Math.Max(maxY, screen.Bounds.Bottom);
                }

                int width = maxX - minX;
                int height = maxY - minY;

                // Tạo bitmap với kích thước tổng
                Bitmap bitmap = new Bitmap(width, height);
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(minX, minY, 0, 0, new Size(width, height));
                }

                _logger.LogInformation($"Screenshot captured: {width}x{height}");
                return bitmap;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing screenshot");
                throw;
            }
        }

        /// <summary>
        /// Chụp màn hình chính
        /// </summary>
        public Bitmap CapturePrimaryScreen()
        {
            try
            {
                var bounds = Screen.PrimaryScreen.Bounds;
                Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height);
                
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size);
                }

                _logger.LogInformation($"Primary screenshot captured: {bounds.Width}x{bounds.Height}");
                return bitmap;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing primary screenshot");
                throw;
            }
        }

        /// <summary>
        /// Chụp màn hình và upload lên server
        /// </summary>
        public async Task<UploadResult> CaptureAndUploadAsync(long attemptId, bool captureAll = true)
        {
            try
            {
                _logger.LogInformation($"Starting screenshot capture and upload for attemptId: {attemptId}");

                // Chụp màn hình
                Bitmap screenshot = captureAll ? CaptureAllScreens() : CapturePrimaryScreen();

                // Upload lên server
                var result = await UploadScreenshotAsync(attemptId, screenshot);

                screenshot.Dispose();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CaptureAndUploadAsync");
                return new UploadResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Upload screenshot lên server qua API
        /// </summary>
        private async Task<UploadResult> UploadScreenshotAsync(long attemptId, Bitmap screenshot)
        {
            try
            {
                // Convert bitmap to byte array
                byte[] imageBytes;
                using (var ms = new MemoryStream())
                {
                    screenshot.Save(ms, ImageFormat.Jpeg);
                    imageBytes = ms.ToArray();
                }

                _logger.LogInformation($"Screenshot size: {imageBytes.Length / 1024}KB");

                // Tạo multipart form data
                using var content = new MultipartFormDataContent();
                using var imageContent = new ByteArrayContent(imageBytes);
                imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
                
                var fileName = $"screenshot_{attemptId}_{DateTime.UtcNow:yyyyMMddHHmmss}.jpg";
                content.Add(imageContent, "file", fileName);
                content.Add(new StringContent($"{{\"type\":\"screenshot\",\"timestamp\":\"{DateTime.UtcNow:O}\"}}"), "meta");

                // Lấy base URL và token
                var baseUrl = Environment.GetEnvironmentVariable("MoniTest_BACKEND_URL") ?? "http://localhost:5045";
                if (baseUrl.EndsWith("/")) baseUrl = baseUrl.TrimEnd('/');

                var url = $"{baseUrl}/api/Upload/attempt/{attemptId}";
                var token = AppHttpSession.Token;

                // Gửi request
                _httpClient.DefaultRequestHeaders.Clear();
                if (!string.IsNullOrEmpty(token))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                _logger.LogInformation($"Uploading to: {url}");
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Upload failed: {response.StatusCode}, {errorContent}");
                    return new UploadResult
                    {
                        Success = false,
                        ErrorMessage = $"Upload failed: {response.StatusCode}"
                    };
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Upload response: {responseContent}");

                // Parse response
                var jsonDoc = System.Text.Json.JsonDocument.Parse(responseContent);
                var root = jsonDoc.RootElement;

                return new UploadResult
                {
                    Success = true,
                    ImageId = root.GetProperty("id").GetInt64(),
                    ImageUrl = root.GetProperty("url").GetString() ?? "",
                    PublicId = root.TryGetProperty("publicId", out var pubId) ? pubId.GetString() : null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading screenshot");
                return new UploadResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Lưu screenshot vào file local (cho debug)
        /// </summary>
        public string SaveToFile(Bitmap screenshot, string? filename = null)
        {
            try
            {
                if (filename == null)
                {
                    filename = $"screenshot_{DateTime.Now:yyyyMMddHHmmss}.jpg";
                }

                var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "MonitoringScreenshots");
                Directory.CreateDirectory(folder);

                var filePath = Path.Combine(folder, filename);
                screenshot.Save(filePath, ImageFormat.Jpeg);

                _logger.LogInformation($"Screenshot saved to: {filePath}");
                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving screenshot to file");
                throw;
            }
        }
    }

    public class UploadResult
    {
        public bool Success { get; set; }
        public long ImageId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string? PublicId { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
