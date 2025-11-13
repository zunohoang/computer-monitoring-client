using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComputerMonitoringClient.Dtos
{
    public class DeviceDetailDto
    {
        public string DeviceName { get; set; } = string.Empty;
        public string OSVersion { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Architecture { get; set; } = string.Empty;
        public string IPAddress { get; set; } = string.Empty;
        public double Latitude { get; set; } = 0.0;
        public double Longitude { get; set; } = 0.0;
        public string Location { get; set; } = string.Empty;

    }
}
