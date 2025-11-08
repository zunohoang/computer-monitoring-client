using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComputerMonitoringClient.Dtos
{
    /// <summary>
    /// DTO for joining a contest room
    /// </summary>
    public class JoinRoomRequest
    {
        public string accessCode { get; set; } = string.Empty;
        public int sbd { get; set; }
        public string ipAddress { get; set; } = string.Empty;
        public string location { get; set; } = string.Empty;

        public JoinRoomRequest()
        {
        }

        public JoinRoomRequest(string accessCode, int sbd, string ipAddress, string location)
        {
            this.accessCode = accessCode;
            this.sbd = sbd;
            this.ipAddress = ipAddress;
            this.location = location;
        }
    }
}
