using Microsoft.VisualBasic.ApplicationServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComputerMonitoringClient.Dtos
{
    public class ProcessDto
    {
        public int? Id { get; set; }
        public int? Pid { get; set; }
        public int? ParentPid { get; set; }
        public string? Name { get; set; }
        public DateTime? Timestamp { get; set; }
        public string? Status { get; set; }
        public string? Data { get; set; }
    }

    // DTO để gửi process changes lên server
    public class ProcessChangeDto
    {
        public int Pid { get; set; }
        public string Name { get; set; } = string.Empty;
        public int ParentPid { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
