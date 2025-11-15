using System;
using System.Diagnostics;
using ComputerMonitoringClient.Utils;
using Microsoft.Extensions.Logging;

namespace ComputerMonitoringClient.Services
{
    /// <summary>
    /// Service xử lý lệnh kill process từ server
    /// </summary>
    public class ProcessKillerService
    {
        private static readonly Lazy<ProcessKillerService> _instance =
            new(() => new ProcessKillerService());
        public static ProcessKillerService Instance => _instance.Value;

        private readonly ILogger<ProcessKillerService> _logger = LoggerProvider.CreateLogger<ProcessKillerService>();
        private readonly ProcessService _processService = new ProcessService();

        private ProcessKillerService() { }

        /// <summary>
        /// Đăng ký event handlers với MonitoringHubClient
        /// </summary>
        public void Initialize()
        {
            _logger.LogInformation("[ProcessKillerService] Initializing...");

            MonitoringHubClient.Instance.OnKillProcessByName += HandleKillProcessByName;
            MonitoringHubClient.Instance.OnKillProcessByPid += HandleKillProcessByPid;

            _logger.LogInformation("[ProcessKillerService] Initialized successfully");
        }

        /// <summary>
        /// Hủy đăng ký event handlers
        /// </summary>
        public void Shutdown()
        {
            _logger.LogInformation("[ProcessKillerService] Shutting down...");

            MonitoringHubClient.Instance.OnKillProcessByName -= HandleKillProcessByName;
            MonitoringHubClient.Instance.OnKillProcessByPid -= HandleKillProcessByPid;

            _logger.LogInformation("[ProcessKillerService] Shutdown complete");
        }

        /// <summary>
        /// Xử lý lệnh kill process theo tên
        /// </summary>
        private void HandleKillProcessByName(string processName)
        {
            try
            {
                _logger.LogInformation($"[ProcessKillerService] Attempting to kill process: {processName}");

                var killedCount = _processService.KillProcessByName(processName);

                if (killedCount > 0)
                {
                    _logger.LogInformation($"[ProcessKillerService] Successfully killed {killedCount} instance(s) of '{processName}'");
                }
                else
                {
                    _logger.LogWarning($"[ProcessKillerService] No instances of '{processName}' found or failed to kill");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[ProcessKillerService] Error killing process '{processName}'");
            }
        }

        /// <summary>
        /// Xử lý lệnh kill process theo PID
        /// </summary>
        private void HandleKillProcessByPid(int pid)
        {
            try
            {
                _logger.LogInformation($"[ProcessKillerService] Attempting to kill process PID: {pid}");

                var success = _processService.KillProcessByPid(pid);

                if (success)
                {
                    _logger.LogInformation($"[ProcessKillerService] Successfully killed process PID {pid}");
                }
                else
                {
                    _logger.LogWarning($"[ProcessKillerService] Failed to kill process PID {pid} or process not found");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[ProcessKillerService] Error killing process PID {pid}");
            }
        }
    }
}
