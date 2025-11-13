using System;
using System.Diagnostics;
using System.Linq;
using ComputerMonitoringClient.Configs;

namespace ComputerMonitoringClient.Services
{
    public class ProcessBlockerService
    {
        private readonly ProcessService _processService;
        
        public event Action<string, int>? ProcessBlocked; // (processName, pid)
        
        public ProcessBlockerService(ProcessService processService)
        {
            _processService = processService;
        }

        /// <summary>
        /// Bắt đầu giám sát và kill các process trong blacklist
        /// </summary>
        public void StartBlocking()
        {
            _processService.ProcessesChangedDetailed += OnProcessesChanged;
        }

        /// <summary>
        /// Dừng giám sát
        /// </summary>
        public void StopBlocking()
        {
            _processService.ProcessesChangedDetailed -= OnProcessesChanged;
        }

        private void OnProcessesChanged(System.Collections.Generic.List<Dtos.ProcessDto> addedProcesses, System.Collections.Generic.List<Dtos.ProcessDto> removedProcesses)
        {
            // Kiểm tra các process mới được mở
            foreach (var proc in addedProcesses)
            {
                if (IsBlacklisted(proc.Name))
                {
                    KillProcess(proc.Pid ?? 0, proc.Name ?? "unknown");
                }
            }
        }

        /// <summary>
        /// Kiểm tra xem process name có trong blacklist không
        /// </summary>
        private bool IsBlacklisted(string? processName)
        {
            if (string.IsNullOrEmpty(processName)) return false;

            return ContestConfig.ProcessBlackList.Any(blacklisted => 
                processName.ToLower().Contains(blacklisted.ToLower()));
        }

        /// <summary>
        /// Kill process theo PID
        /// </summary>
        private void KillProcess(int pid, string processName)
        {
            try
            {
                var process = Process.GetProcessById(pid);
                process.Kill();
                process.WaitForExit(2000); // Đợi tối đa 2 giây
                
                Debug.WriteLine($"[ProcessBlocker] Killed blacklisted process: {processName} (PID: {pid})");
                ProcessBlocked?.Invoke(processName, pid);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ProcessBlocker] Failed to kill process {processName} (PID: {pid}): {ex.Message}");
            }
        }
    }
}
