using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using ComputerMonitoringClient.Models;
using System.Collections.Concurrent;
using System.Threading;

namespace ComputerMonitoringClient.Services
{
    internal class ProcessService
    {
        public static ProcessService Instance { get; } = new ProcessService();

        // Performance optimization: Cache for processes and system resources
        private List<Dtos.Proccess> cachedProcesses = new List<Dtos.Proccess>();
        private DateTime lastProcessUpdate = DateTime.MinValue;
        private string cachedSystemResources = "";
        private DateTime lastResourceUpdate = DateTime.MinValue;
        
        // Cache expiration times (in seconds)
        private const int PROCESS_CACHE_DURATION = 3; // 3 seconds
        private const int RESOURCE_CACHE_DURATION = 5; // 5 seconds
        
        // Suspicious process names for quick lookup
        private static readonly HashSet<string> SuspiciousProcessNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "chrome", "firefox", "msedge", "iexplore", // Browsers
            "skype", "zoom", "teams", "discord", "telegram", // Chat/Call apps
            "teamviewer", "anydesk", "vnc", "rdp", // Remote desktop
            "notepad++", "sublimetext", "vscode", "atom", "vim", // Code editors
            "calculator", "calc", "charmap", // Calculator and utilities
            "cmd", "powershell", "bash", "wsl", // Command line
            "regedit", "taskmgr", "msconfig", // System tools
            "utorrent", "bittorrent", "qbittorrent", // P2P software
            "cheatengine", "wireshark", "fiddler", // Hacking/Network tools
            "virtualbox", "vmware", "hyper-v" // Virtualization
        };

        // Performance counters (initialized once)
        private PerformanceCounter cpuCounter;
        private PerformanceCounter memCounter;
        private bool performanceCountersInitialized = false;

        public string GetSystemInfo()
        {
            return "System Information: [Placeholder]";
        }

        /// <summary>
        /// Lấy danh sách tất cả các tiến trình đang chạy (optimized with caching)
        /// </summary>
        public List<Dtos.Proccess> GetRunningProcesses()
        {
            // Check cache first
            if (DateTime.Now.Subtract(lastProcessUpdate).TotalSeconds < PROCESS_CACHE_DURATION)
            {
                return new List<Dtos.Proccess>(cachedProcesses);
            }

            var processes = new List<Dtos.Proccess>();

            try
            {
                var systemProcesses = Process.GetProcesses();

                // Use parallel processing for better performance
                var processData = new ConcurrentBag<Dtos.Proccess>();
                
                Parallel.ForEach(systemProcesses, proc =>
                {
                    try
                    {
                        var process = CreateProcessFromSystemProcess(proc);
                        if (process != null)
                        {
                            processData.Add(process);
                        }
                    }
                    catch
                    {
                        // Skip processes that can't be accessed
                    }
                });

                processes = processData.OrderBy(p => p.Name).ToList();

                // Update cache
                cachedProcesses = new List<Dtos.Proccess>(processes);
                lastProcessUpdate = DateTime.Now;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy danh sách tiến trình: {ex.Message}");
            }

            return processes;
        }

        /// <summary>
        /// Tạo đối tượng Process từ System.Diagnostics.Process với optimized property access
        /// </summary>
        private Dtos.Proccess CreateProcessFromSystemProcess(Process proc)
        {
            try
            {
                var process = new Dtos.Proccess
                {
                    Pid = proc.Id,
                    Name = proc.ProcessName ?? "Unknown",
                    StartTime = GetProcessStartTimeSafe(proc),
                    Status = Dtos.Proccess.ProcessStatus.Running
                };

                // Optimize: Get only essential information quickly
                process.Description = GetProcessDescriptionFast(proc);
                process.WindowTitle = GetWindowTitleSafe(proc);
                
                // Expensive operations - get these only if needed
                process.MemoryUsage = GetMemoryUsageSafe(proc);
                process.ThreadCount = GetThreadCountSafe(proc);
                
                // Check if suspicious
                process.IsSuspicious = IsSuspiciousProcess(process.Name);
                
                process.Metadata = $"Memory: {process.MemoryUsage / (1024 * 1024):F1}MB, Threads: {process.ThreadCount}";

                return process;
            }
            catch
            {
                return null; // Skip problematic processes
            }
        }

        /// <summary>
        /// Fast suspicious process check using HashSet
        /// </summary>
        private bool IsSuspiciousProcess(string processName)
        {
            if (string.IsNullOrEmpty(processName))
                return false;
                
            return SuspiciousProcessNames.Contains(processName) ||
                   SuspiciousProcessNames.Any(suspicious => processName.IndexOf(suspicious, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        /// <summary>
        /// Lấy thông tin chi tiết về các tiến trình đang chạy (cached)
        /// </summary>
        public string GetProcessesInfo()
        {
            try
            {
                var processes = GetRunningProcesses(); // Uses cache
                var sb = new StringBuilder();
                
                sb.AppendLine($"THÔNG TIN TIẾN TRÌNH HỆ THỐNG");
                sb.AppendLine($"Tổng số tiến trình: {processes.Count}");
                sb.AppendLine($"Thời gian quét: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine();

                // Top 15 processes by name (increased from 10)
                sb.AppendLine("TOP 15 TIẾN TRÌNH ĐANG CHẠY:");
                var topProcesses = processes.Take(15).ToList();
                
                foreach (var proc in topProcesses)
                {
                    var suspiciousMarker = proc.IsSuspicious ? " ⚠️" : "";
                    sb.AppendLine($"• {proc.Name} (PID: {proc.Pid}){suspiciousMarker} - {proc.Description}");
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"Lỗi khi lấy thông tin tiến trình: {ex.Message}";
            }
        }

        /// <summary>
        /// Kiểm tra tiến trình đáng ngờ (optimized)
        /// </summary>
        public List<Dtos.Proccess> GetSuspiciousProcesses()
        {
            var allProcesses = GetRunningProcesses(); // Uses cache
            return allProcesses.Where(p => p.IsSuspicious).ToList();
        }

        /// <summary>
        /// Tạo MonitoringLog cho tiến trình mới (optimized)
        /// </summary>
        public MonitoringLog MonitorProcessChanges(List<Dtos.Proccess> previousProcesses)
        {
            try
            {
                var currentProcesses = GetRunningProcesses(); // Uses cache
                
                // Use HashSet for faster lookups
                var prevPids = new HashSet<int>(previousProcesses.Select(p => p.Pid));
                var currentPids = new HashSet<int>(currentProcesses.Select(p => p.Pid));
                
                // Find new and ended processes
                var newProcesses = currentProcesses.Where(p => !prevPids.Contains(p.Pid)).ToList();
                var endedProcesses = previousProcesses.Where(p => !currentPids.Contains(p.Pid)).ToList();

                if (newProcesses.Any() || endedProcesses.Any())
                {
                    var sb = new StringBuilder();
                    
                    if (newProcesses.Any())
                    {
                        sb.AppendLine($"Tiến trình mới ({newProcesses.Count}):");
                        foreach (var proc in newProcesses.Take(5))
                        {
                            var suspiciousMarker = proc.IsSuspicious ? " ⚠️" : "";
                            sb.AppendLine($"  + {proc.Name} (PID: {proc.Pid}){suspiciousMarker}");
                        }
                        if (newProcesses.Count > 5)
                        {
                            sb.AppendLine($"  ... và {newProcesses.Count - 5} tiến trình khác");
                        }
                    }

                    if (endedProcesses.Any())
                    {
                        sb.AppendLine($"Tiến trình kết thúc ({endedProcesses.Count}):");
                        foreach (var proc in endedProcesses.Take(5))
                        {
                            sb.AppendLine($"  - {proc.Name} (PID: {proc.Pid})");
                        }
                        if (endedProcesses.Count > 5)
                        {
                            sb.AppendLine($"  ... và {endedProcesses.Count - 5} tiến trình khác");
                        }
                    }

                    // Check for new suspicious processes
                    var newSuspicious = newProcesses.Where(p => p.IsSuspicious).ToList();
                    LogType logType = LogType.Info;
                    
                    if (newSuspicious.Any())
                    {
                        logType = LogType.Warning;
                        sb.AppendLine($"⚠️ Phát hiện {newSuspicious.Count} tiến trình đáng ngờ mới!");
                    }

                    return new MonitoringLog(logType, "Thay đổi tiến trình", sb.ToString().Trim());
                }

                return null; // No changes
            }
            catch (Exception ex)
            {
                return new MonitoringLog(LogType.Error, "Lỗi giám sát tiến trình", ex.Message);
            }
        }

        /// <summary>
        /// Lấy thông tin CPU và Memory usage (optimized with caching and no Thread.Sleep)
        /// </summary>
        public string GetSystemResourceInfo()
        {
            // Check cache first
            if (DateTime.Now.Subtract(lastResourceUpdate).TotalSeconds < RESOURCE_CACHE_DURATION)
            {
                return cachedSystemResources;
            }

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("THÔNG TIN TÀI NGUYÊN HỆ THỐNG:");
                
                // Initialize performance counters only once
                if (!performanceCountersInitialized)
                {
                    InitializePerformanceCounters();
                }

                // CPU Information (without Thread.Sleep)
                if (cpuCounter != null)
                {
                    try
                    {
                        var cpuUsage = cpuCounter.NextValue();
                        sb.AppendLine($"• CPU Usage: {cpuUsage:F1}%");
                    }
                    catch
                    {
                        sb.AppendLine("• CPU Usage: N/A");
                    }
                }
                else
                {
                    sb.AppendLine("• CPU Usage: N/A");
                }

                // Memory Information
                if (memCounter != null)
                {
                    try
                    {
                        var availableMemory = memCounter.NextValue();
                        sb.AppendLine($"• Available Memory: {availableMemory:F0} MB");
                    }
                    catch
                    {
                        sb.AppendLine("• Available Memory: N/A");
                    }
                }
                else
                {
                    sb.AppendLine("• Available Memory: N/A");
                }

                // Process count (from cache)
                var processCount = cachedProcesses.Count > 0 ? cachedProcesses.Count : Process.GetProcesses().Length;
                sb.AppendLine($"• Total Processes: {processCount}");

                // Cache the result
                cachedSystemResources = sb.ToString();
                lastResourceUpdate = DateTime.Now;
                
                return cachedSystemResources;
            }
            catch (Exception ex)
            {
                var errorMessage = $"Lỗi khi lấy thông tin tài nguyên: {ex.Message}";
                cachedSystemResources = errorMessage;
                lastResourceUpdate = DateTime.Now;
                return errorMessage;
            }
        }

        /// <summary>
        /// Initialize performance counters once for better performance
        /// </summary>
        private void InitializePerformanceCounters()
        {
            try
            {
                cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                cpuCounter.NextValue(); // First call to initialize
                
                memCounter = new PerformanceCounter("Memory", "Available MBytes");
                
                performanceCountersInitialized = true;
            }
            catch
            {
                // Performance counters might not be available in some environments
                performanceCountersInitialized = true; // Mark as initialized even if failed
            }
        }

        // Safe property access methods to avoid exceptions and improve performance
        private string GetProcessDescriptionFast(Process proc)
        {
            try
            {
                var title = proc.MainWindowTitle;
                if (!string.IsNullOrEmpty(title))
                {
                    return title.Length > 50 ? title.Substring(0, 50) + "..." : title;
                }
                return proc.ProcessName ?? "Unknown";
            }
            catch
            {
                return "N/A";
            }
        }

        private DateTime GetProcessStartTimeSafe(Process proc)
        {
            try
            {
                return proc.StartTime;
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        private string GetWindowTitleSafe(Process proc)
        {
            try
            {
                return proc.MainWindowTitle ?? "";
            }
            catch
            {
                return "";
            }
        }

        private long GetMemoryUsageSafe(Process proc)
        {
            try
            {
                return proc.WorkingSet64;
            }
            catch
            {
                return 0;
            }
        }

        private int GetThreadCountSafe(Process proc)
        {
            try
            {
                return proc.Threads.Count;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Force refresh cache (useful for manual refresh)
        /// </summary>
        public void RefreshCache()
        {
            lastProcessUpdate = DateTime.MinValue;
            lastResourceUpdate = DateTime.MinValue;
            cachedProcesses.Clear();
            cachedSystemResources = "";
        }

        /// <summary>
        /// Get process count without full enumeration
        /// </summary>
        public int GetProcessCount()
        {
            if (cachedProcesses.Count > 0 && DateTime.Now.Subtract(lastProcessUpdate).TotalSeconds < PROCESS_CACHE_DURATION)
            {
                return cachedProcesses.Count;
            }
            
            try
            {
                return Process.GetProcesses().Length;
            }
            catch
            {
                return 0;
            }
        }

        // Legacy methods (keeping for backward compatibility)
        private string GetProcessDescription(Process proc) => GetProcessDescriptionFast(proc);
        private DateTime GetProcessStartTime(Process proc) => GetProcessStartTimeSafe(proc);
        private string GetProcessMetadata(Process proc)
        {
            try
            {
                var memory = GetMemoryUsageSafe(proc);
                var threads = GetThreadCountSafe(proc);
                var hasWindow = !string.IsNullOrEmpty(GetWindowTitleSafe(proc));
                
                return $"Memory: {memory / (1024 * 1024):F1}MB, Threads: {threads}" + 
                       (hasWindow ? ", HasWindow" : "");
            }
            catch
            {
                return "N/A";
            }
        }
    }
}
