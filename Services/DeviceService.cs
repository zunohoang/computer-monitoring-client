using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputerMonitoringClient.Dtos;

namespace ComputerMonitoringClient.Services
{
    internal class DeviceService
    {
        public static DeviceService Instance { get; } = new DeviceService();

        public DeviceService() { }

        public DeviceDetailDto GetDeviceDetail()
        {
            var deviceName = Environment.MachineName;
            var osVersion = Environment.OSVersion.ToString();
            var userName = Environment.UserName;
            var architecture = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString();

            // var details = new StringBuilder();
            // details.AppendLine($"Device Name: {deviceName}");
            // details.AppendLine($"OS Version: {osVersion}");
            // details.AppendLine($"User Name: {userName}");
            // details.AppendLine($"Architecture: {architecture}");

            return new DeviceDetailDto
            {
                DeviceName = deviceName,
                OSVersion = osVersion,
                UserName = userName,
                Architecture = architecture
            };
        }
    }
}
