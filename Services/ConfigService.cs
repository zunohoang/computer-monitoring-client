using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputerMonitoringClient.Networks;

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

        // get blacklist
        public async Task<List<string>> GetProcessBlackList()
        {
            try
            {
                var response = await ApiClient.Instance.GetAsync<List<string>>(
                    $"Contest/{AppHttpSession.CurrentContestId ?? 0}/process-blacklist"
                );                
                return response ?? new List<string>();
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., log the error)
                return new List<string>();
            }
        }
    }
}
