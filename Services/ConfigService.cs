using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComputerMonitoringClient.Services
{
    internal class ConfigService
    {
        public static ConfigService Instance { get; } = new ConfigService();

        public ConfigService() { }

        public string GetConfiguration()
        {
            return "Configuration data placeholder";
        }
    }
}
