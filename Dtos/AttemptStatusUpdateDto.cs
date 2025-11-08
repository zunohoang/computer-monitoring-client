using System;

namespace ComputerMonitoringClient.Dtos
{
    /// <summary>
    /// DTO for attempt status update from SignalR
    /// </summary>
    public class AttemptStatusUpdateDto
    {
        public int attemptId { get; set; }
        public string status { get; set; } = string.Empty;
        public AttemptDto? attempt { get; set; }
        public DateTime timestamp { get; set; }
    }

    /// <summary>
    /// Attempt details from server
    /// </summary>
    public class AttemptDto
    {
        public int id { get; set; }
        public int roomId { get; set; }
        public int contestId { get; set; }
        public int sbd { get; set; }
        public string fullName { get; set; } = string.Empty;
        public string status { get; set; } = string.Empty;
        public string? ipAddress { get; set; }
        public string? location { get; set; }
        public DateTime? startTime { get; set; }
        public DateTime? endTime { get; set; }
        public DateTime createdAt { get; set; }
        public DateTime updatedAt { get; set; }
    }
}
