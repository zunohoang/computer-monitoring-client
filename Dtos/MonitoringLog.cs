using System;

namespace ComputerMonitoringClient.Models
{
    /// <summary>
    /// Enum cho lo?i log
    /// </summary>
    public enum LogType
    {
        Info,
        Warning,
        Error,
        Success
    }

    /// <summary>
    /// Model ð?i di?n cho m?t log giám sát
    /// </summary>
    public class MonitoringLog
    {
        public DateTime Timestamp { get; set; }
        public LogType Type { get; set; }
        public string Message { get; set; }
        public string Details { get; set; }

        public MonitoringLog()
        {
            Timestamp = DateTime.Now;
            Type = LogType.Info;
        }

        public MonitoringLog(LogType type, string message, string details = "")
        {
            Timestamp = DateTime.Now;
            Type = type;
            Message = message;
            Details = details;
        }

        public override string ToString()
        {
            return $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Type.ToString().ToUpper()}] {Message}";
        }
    }
}
