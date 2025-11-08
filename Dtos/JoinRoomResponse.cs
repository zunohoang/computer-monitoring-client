using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComputerMonitoringClient.Dtos
{
    /// <summary>
    /// Room join status
    /// </summary>
    public enum RoomJoinStatus
    {
        Pending,    // Waiting for approval
        Approved,   // Approved to join
        Rejected    // Rejected from joining
    }

    /// <summary>
    /// DTO for join room response
    /// </summary>
    public class JoinRoomResponse
    {
        public int attemptId { get; set; }
        public int roomId { get; set; }
        public int contestId { get; set; }
        public int sbd { get; set; }
        public string fullName { get; set; } = string.Empty;
        public string status { get; set; } = string.Empty;
        public string message { get; set; } = string.Empty;
        public string token { get; set; } = string.Empty;

        public JoinRoomResponse()
        {
        }

        /// <summary>
        /// Check if status is pending
        /// </summary>
        public bool IsPending => status.Equals("pending", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Check if status is approved
        /// </summary>
        public bool IsApproved => status.Equals("approved", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Check if status is rejected
        /// </summary>
        public bool IsRejected => status.Equals("rejected", StringComparison.OrdinalIgnoreCase);
    }
}
