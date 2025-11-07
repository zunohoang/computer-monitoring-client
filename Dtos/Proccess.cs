using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComputerMonitoringClient.Dtos
{
    public class Proccess // Keeping the original typo for backward compatibility
    {
        public Proccess() 
        {
            StartTime = DateTime.Now;
            Status = ProcessStatus.Running;
        }

        public int Id { get; set; }

        public int Pid { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string Metadata { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public ProcessStatus Status { get; set; }

        // Additional properties for process monitoring
        public long MemoryUsage { get; set; } // Memory in bytes

        public int ThreadCount { get; set; }

        public string WindowTitle { get; set; }

        public bool HasWindow => !string.IsNullOrEmpty(WindowTitle);

        public bool IsSuspicious { get; set; }

        public string FilePath { get; set; }

        public enum ProcessStatus
        {
            Running,
            Stopped,
            Suspended,
            Unknown
        }

        public override string ToString()
        {
            var statusText = IsSuspicious ? " [ĐÁNG NGỜ]" : "";
            var memoryText = MemoryUsage > 0 ? $" - {MemoryUsage / (1024 * 1024):F1}MB" : "";
            
            return $"PID: {Pid} | {Name}{statusText}{memoryText} - {Description}";
        }

        public string GetDetailedInfo()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Process ID: {Pid}");
            sb.AppendLine($"Name: {Name}");
            sb.AppendLine($"Description: {Description}");
            sb.AppendLine($"Start Time: {StartTime:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Status: {Status}");
            
            if (MemoryUsage > 0)
                sb.AppendLine($"Memory Usage: {MemoryUsage / (1024 * 1024):F1} MB");
            
            if (ThreadCount > 0)
                sb.AppendLine($"Thread Count: {ThreadCount}");
            
            if (HasWindow)
                sb.AppendLine($"Window Title: {WindowTitle}");
            
            if (!string.IsNullOrEmpty(FilePath))
                sb.AppendLine($"File Path: {FilePath}");
            
            if (IsSuspicious)
                sb.AppendLine("⚠️ TIẾN TRÌNH ĐÁNG NGỜ");
                
            return sb.ToString();
        }
    }
}
