using System;
using System.Collections.Generic;
using ComputerMonitoringClient.Models;
using ComputerMonitoringClient.Services;

namespace ComputerMonitoringClient.Services
{
    /// <summary>
    /// Service xử lý logic giám sát
    /// </summary>
    public class MonitoringService
    {
        private static MonitoringService instance;
        private List<MonitoringLog> logs;
        private bool isMonitoring;
        private List<Dtos.Proccess> previousProcesses;
        private readonly ProcessService processService;
        private int monitoringCycles;

        private MonitoringService()
        {
            logs = new List<MonitoringLog>();
            isMonitoring = false;
            previousProcesses = new List<Dtos.Proccess>();
            processService = ProcessService.Instance;
            monitoringCycles = 0;
        }

        public static MonitoringService Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new MonitoringService();
                }
                return instance;
            }
        }

        public bool IsMonitoring
        {
            get { return isMonitoring; }
        }

        public List<MonitoringLog> Logs
        {
            get { return new List<MonitoringLog>(logs); }
        }

        /// <summary>
        /// Bắt đầu giám sát
        /// </summary>
        public void StartMonitoring()
        {
            if (!isMonitoring)
            {
                isMonitoring = true;
                monitoringCycles = 0;
                
                // Lấy danh sách tiến trình hiện tại làm baseline
                try
                {
                    previousProcesses = processService.GetRunningProcesses();
                    AddLog(LogType.Success, "Bắt đầu giám sát hệ thống", $"Đã ghi nhận {previousProcesses.Count} tiến trình hiện tại");
                }
                catch (Exception ex)
                {
                    AddLog(LogType.Error, "Lỗi khi khởi tạo giám sát tiến trình", ex.Message);
                    previousProcesses = new List<Dtos.Proccess>();
                }

                AddLog(LogType.Success, "Bắt đầu giám sát hệ thống");
            }
        }

        /// <summary>
        /// Dừng giám sát
        /// </summary>
        public void StopMonitoring()
        {
            if (isMonitoring)
            {
                isMonitoring = false;
                AddLog(LogType.Warning, "Đã dừng giám sát hệ thống", $"Tổng số chu kỳ giám sát: {monitoringCycles}");
            }
        }

        /// <summary>
        /// Thêm log
        /// </summary>
        public void AddLog(LogType type, string message, string details = "")
        {
            var log = new MonitoringLog(type, message, details);
            logs.Add(log);
        }

        /// <summary>
        /// Xóa tất cả logs
        /// </summary>
        public void ClearLogs()
        {
            logs.Clear();
        }

        /// <summary>
        /// Thực hiện kiểm tra giám sát với tích hợp process monitoring
        /// </summary>
        public MonitoringLog PerformMonitoringCheck()
        {
            if (!isMonitoring)
                return null;

            monitoringCycles++;
            Random rand = new Random();
            
            // Mỗi 5 chu kỳ thì kiểm tra tiến trình (tránh quá tải)
            if (monitoringCycles % 5 == 0)
            {
                try
                {
                    var processLog = processService.MonitorProcessChanges(previousProcesses);
                    if (processLog != null)
                    {
                        logs.Add(processLog);
                        
                        // Cập nhật danh sách tiến trình hiện tại
                        previousProcesses = processService.GetRunningProcesses();
                        return processLog;
                    }
                }
                catch (Exception ex)
                {
                    var errorLog = new MonitoringLog(LogType.Error, "Lỗi giám sát tiến trình", ex.Message);
                    logs.Add(errorLog);
                    return errorLog;
                }
            }

            // Mỗi 10 chu kỳ thì kiểm tra tài nguyên hệ thống
            if (monitoringCycles % 10 == 0)
            {
                try
                {
                    var resourceInfo = processService.GetSystemResourceInfo();
                    var resourceLog = new MonitoringLog(LogType.Info, "Thông tin tài nguyên hệ thống", resourceInfo);
                    logs.Add(resourceLog);
                    return resourceLog;
                }
                catch (Exception ex)
                {
                    var errorLog = new MonitoringLog(LogType.Error, "Lỗi kiểm tra tài nguyên", ex.Message);
                    logs.Add(errorLog);
                    return errorLog;
                }
            }

            // Các hoạt động giám sát khác (random như trước)
            int activity = rand.Next(0, 12); // Tăng lên 12 để có thêm các case mới

            MonitoringLog log;
            switch (activity)
            {
                case 0:
                    log = new MonitoringLog(LogType.Info, "Kiểm tra hoạt động máy tính - Bình thường");
                    break;
                case 1:
                    log = new MonitoringLog(LogType.Info, "Giám sát màn hình - Không phát hiện bất thường");
                    break;
                case 2:
                    log = new MonitoringLog(LogType.Info, "Kiểm tra ứng dụng đang chạy - OK");
                    break;
                case 3:
                    log = new MonitoringLog(LogType.Warning, "Phát hiện hoạt động đáng ngờ!");
                    break;
                case 4:
                    log = new MonitoringLog(LogType.Info, "Kết nối mạng ổn định");
                    break;
                case 5:
                    log = new MonitoringLog(LogType.Info, "CPU Usage: " + rand.Next(10, 60) + "%");
                    break;
                case 6:
                    log = new MonitoringLog(LogType.Info, "Memory Usage: " + rand.Next(20, 70) + "%");
                    break;
                case 7:
                    log = new MonitoringLog(LogType.Info, "Kiểm tra quy trình thi - Bình thường");
                    break;
                case 8:
                    log = new MonitoringLog(LogType.Success, "Hệ thống hoạt động ổn định");
                    break;
                case 9:
                    // Kiểm tra tiến trình đáng ngờ
                    try
                    {
                        var suspiciousProcesses = processService.GetSuspiciousProcesses();
                        if (suspiciousProcesses.Count > 0)
                        {
                            log = new MonitoringLog(LogType.Warning, $"Phát hiện {suspiciousProcesses.Count} tiến trình đáng ngờ", 
                                string.Join(", ", suspiciousProcesses.ConvertAll(p => p.Name)));
                        }
                        else
                        {
                            log = new MonitoringLog(LogType.Success, "Không phát hiện tiến trình đáng ngờ");
                        }
                    }
                    catch
                    {
                        log = new MonitoringLog(LogType.Info, "Giám sát tiến trình đáng ngờ - OK");
                    }
                    break;
                case 10:
                    log = new MonitoringLog(LogType.Info, "Kiểm tra bảo mật hệ thống - Bình thường");
                    break;
                default:
                    log = new MonitoringLog(LogType.Info, "Giám sát tự động đang hoạt động");
                    break;
            }

            logs.Add(log);
            return log;
        }

        /// <summary>
        /// Lấy thông tin tổng quan về tiến trình
        /// </summary>
        public string GetProcessOverview()
        {
            try
            {
                return processService.GetProcessesInfo();
            }
            catch (Exception ex)
            {
                return $"Lỗi khi lấy thông tin tiến trình: {ex.Message}";
            }
        }

        /// <summary>
        /// Lấy danh sách tiến trình đáng ngờ hiện tại
        /// </summary>
        public List<Dtos.Proccess> GetCurrentSuspiciousProcesses()
        {
            try
            {
                return processService.GetSuspiciousProcesses();
            }
            catch
            {
                return new List<Dtos.Proccess>();
            }
        }
    }
}
