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
                        var addedIds = currentIds.Except(_lastProcessIds).ToList();
                        var addedProcesses = addedIds
                            .Select(id => currentProcessMap[id])
                            .ToList();

                        var removedIds = _lastProcessIds.Except(currentIds).ToList();
                        var removedProcesses = removedIds
                            .Where(id => _lastProcessMap.ContainsKey(id))
                            .Select(id => _lastProcessMap[id])
                            .ToList();

                        _lastProcessIds = currentIds;
                        _lastProcessMap = currentProcessMap;

                        ProcessesChanged?.Invoke(processDtos);

                        ProcessesChangedDetailed?.Invoke(addedProcesses, removedProcesses);
                    }

                    await Task.Delay(intervalMs, token);
                }
                catch (TaskCanceledException)
                {
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

        /// <summary>
        /// Kill tiến trình theo tên (kill tất cả processes có tên này)
        /// </summary>
        public int KillProcessByName(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName))
            {
                Debug.WriteLine("[ProcessService] Process name is empty");
                return 0;
            }

            try
            {
                var processes = Process.GetProcessesByName(processName);
                var killedCount = 0;

                foreach (var process in processes)
                {
                    try
                    {
                        Debug.WriteLine($"[ProcessService] Killing process: {process.ProcessName} (PID: {process.Id})");
                        process.Kill();
                        process.WaitForExit(3000); // Đợi tối đa 3 giây
                        killedCount++;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[ProcessService] Failed to kill PID {process.Id}: {ex.Message}");
                    }
                }

                Debug.WriteLine($"[ProcessService] Killed {killedCount} instances of '{processName}'");
                return killedCount;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ProcessService] Error killing process '{processName}': {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Kill tiến trình theo PID
        /// </summary>
        public bool KillProcessByPid(int pid)
        {
            if (pid <= 0)
            {
                Debug.WriteLine("[ProcessService] Invalid PID");
                return false;
            }

            try
            {
                var process = Process.GetProcessById(pid);
                Debug.WriteLine($"[ProcessService] Killing process: {process.ProcessName} (PID: {pid})");
                process.Kill();
                process.WaitForExit(3000);
                Debug.WriteLine($"[ProcessService] Successfully killed PID {pid}");
                return true;
            }
            catch (ArgumentException)
            {
                Debug.WriteLine($"[ProcessService] Process with PID {pid} not found");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ProcessService] Error killing PID {pid}: {ex.Message}");
                return false;
            }
        }
    }
}
