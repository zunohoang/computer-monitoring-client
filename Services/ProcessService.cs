using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using ComputerMonitoringClient.Dtos;

namespace ComputerMonitoringClient.Services
{
    public class ProcessService
    {
        public event Action<List<ProcessDto>>? ProcessesChanged;
        public event Action<List<ProcessDto>, List<ProcessDto>>? ProcessesChangedDetailed; // (added, removed)

        private HashSet<int> _lastProcessIds = new();
        private Dictionary<int, ProcessDto> _lastProcessMap = new();
        private CancellationTokenSource? _cts;

        /// <summary>
        /// Bắt đầu giám sát các tiến trình.
        /// </summary>
        public void StartMonitoring(int intervalMs = 2000)
        {
            StopMonitoring(); // Dừng nếu đang chạy
            _cts = new CancellationTokenSource();
            _ = Task.Run(() => MonitorProcessesAsync(intervalMs, _cts.Token));
        }

        /// <summary>
        /// Dừng giám sát tiến trình.
        /// </summary>
        public void StopMonitoring()
        {
            if (_cts == null) return;

            try
            {
                _cts.Cancel();
                _cts.Dispose();
            }
            catch { /* bỏ qua lỗi dispose */ }
            finally
            {
                _cts = null;
            }
        }

        /// <summary>
        /// Lấy danh sách tiến trình hệ thống.
        /// </summary>
        public List<Process> GetProcesses() => Process.GetProcesses().ToList();

        /// <summary>
        /// Lấy danh sách DTO tiến trình.
        /// </summary>
        public List<ProcessDto> GetProcessDtos()
        {
            var now = DateTime.Now;
            var processDtos = new List<ProcessDto>();

            foreach (var process in Process.GetProcesses())
            {
                try
                {
                    processDtos.Add(new ProcessDto
                    {
                        Id = process.Id,
                        Name = process.ProcessName,
                        Pid = process.Id,
                        ParentPid = null,
                        Timestamp = now,
                        Status = process.Responding ? "Running" : "Not Responding",
                    });
                }
                catch
                {
                    // Có thể process vừa thoát => bỏ qua
                }
            }

            return processDtos;
        }

        /// <summary>
        /// Vòng giám sát liên tục (background).
        /// </summary>
        private async Task MonitorProcessesAsync(int intervalMs, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                
                try
                {
                    var processDtos = GetProcessDtos();
                    var currentProcessMap = processDtos
                        .Where(p => p.Pid.HasValue)
                        .ToDictionary(p => p.Pid!.Value, p => p);
                    
                    var currentIds = currentProcessMap.Keys.ToHashSet();

                    if (!currentIds.SetEquals(_lastProcessIds))
                    {
                        // Tìm tiến trình mới được mở (có trong current nhưng không có trong last)
                        var addedIds = currentIds.Except(_lastProcessIds).ToList();
                        var addedProcesses = addedIds
                            .Select(id => currentProcessMap[id])
                            .ToList();

                        // Tìm tiến trình đã đóng (có trong last nhưng không có trong current)
                        var removedIds = _lastProcessIds.Except(currentIds).ToList();
                        var removedProcesses = removedIds
                            .Where(id => _lastProcessMap.ContainsKey(id))
                            .Select(id => _lastProcessMap[id])
                            .ToList();

                        // Cập nhật state
                        _lastProcessIds = currentIds;
                        _lastProcessMap = currentProcessMap;

                        // Phát sự kiện với danh sách đầy đủ (để backward compatibility)
                        ProcessesChanged?.Invoke(processDtos);
                        
                        // Phát sự kiện chi tiết với danh sách thay đổi
                        ProcessesChangedDetailed?.Invoke(addedProcesses, removedProcesses);
                    }

                    await Task.Delay(intervalMs, token);
                }
                catch (TaskCanceledException)
                {
                    // Task bị hủy, thoát nhẹ nhàng
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ProcessService] Error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Lấy parent process ID (an toàn, không crash).
        /// </summary>
        private int? GetParentProcessIdSafe(int processId)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    $"SELECT ParentProcessId FROM Win32_Process WHERE ProcessId = {processId}");
                using var results = searcher.Get();
                var obj = results.Cast<ManagementObject>().FirstOrDefault();
                return obj != null ? Convert.ToInt32(obj["ParentProcessId"]) : null;
            }
            catch
            {
                return null;
            }
        }
    }
}
