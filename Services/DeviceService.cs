using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ComputerMonitoringClient.Services
{
    internal class DeviceService
    {
        public static DeviceService Instance { get; } = new DeviceService();

        public string GetDeviceInfo()
        {
            string machineName = Environment.MachineName;
            string userName = Environment.UserName;
            string osVersion = Environment.OSVersion.ToString();

            string localIp = GetLocalIPv4();
            string macAddress = GetMacAddress();

            // Vị trí địa lý (chỉ lấy được qua API ngoài, ví dụ ip-api.com)
            string publicIp = GetPublicIP();
            var locationInfo = GetLocationInfo();

            return $@"
                    Device Information:
                    - Machine Name : {machineName}
                    - User Name    : {userName}
                    - OS Version   : {osVersion}
                    - Local IP     : {localIp}
                    - Public IP    : {publicIp}
                    - MAC Address  : {macAddress}
                    - Location Info: {locationInfo}
                    ";
        }

        public string GetLocationInfo()
        {
            try
            {
                using (var client = new WebClient())
                {
                    // Sử dụng dịch vụ miễn phí ip-api.com để lấy thông tin vị trí
                    string response = client.DownloadString("http://ip-api.com/json/");
                    
                    // Parse JSON response (simple parsing for .NET Framework 4.7.2)
                    var locationData = ParseLocationJson(response);
                    
                    if (locationData != null)
                    {
                        return $@"
        • Quốc gia: {locationData.Country}
        • Tỉnh/Thành phố: {locationData.RegionName}
        • Thành phố: {locationData.City}
        • Tọa độ: {locationData.Latitude}, {locationData.Longitude}
        • Múi giờ: {locationData.Timezone}
        • Nhà mạng: {locationData.ISP}";
                    }
                }
            }
            catch (Exception ex)
            {
                return $"Không thể lấy thông tin vị trí: {ex.Message}";
            }
            
            return "Không có thông tin vị trí";
        }

        public LocationData GetDetailedLocationInfo()
        {
            try
            {
                using (var client = new WebClient())
                {
                    string response = client.DownloadString("http://ip-api.com/json/");
                    return ParseLocationJson(response);
                }
            }
            catch
            {
                return new LocationData
                {
                    Country = "N/A",
                    RegionName = "N/A", 
                    City = "N/A",
                    Latitude = 0,
                    Longitude = 0,
                    Timezone = "N/A",
                    ISP = "N/A"
                };
            }
        }

        private LocationData ParseLocationJson(string json)
        {
            try
            {
                // Simple JSON parsing for basic location data
                var locationData = new LocationData();
                
                // Remove braces and split by comma
                json = json.Trim('{', '}');
                var pairs = json.Split(',');
                
                foreach (var pair in pairs)
                {
                    var keyValue = pair.Split(':');
                    if (keyValue.Length == 2)
                    {
                        var key = keyValue[0].Trim().Trim('"');
                        var value = keyValue[1].Trim().Trim('"');
                        
                        switch (key.ToLower())
                        {
                            case "country":
                                locationData.Country = value;
                                break;
                            case "regionname":
                                locationData.RegionName = value;
                                break;
                            case "city":
                                locationData.City = value;
                                break;
                            case "lat":
                                if (double.TryParse(value, out double lat))
                                    locationData.Latitude = lat;
                                break;
                            case "lon":
                                if (double.TryParse(value, out double lon))
                                    locationData.Longitude = lon;
                                break;
                            case "timezone":
                                locationData.Timezone = value;
                                break;
                            case "isp":
                                locationData.ISP = value;
                                break;
                        }
                    }
                }
                
                return locationData;
            }
            catch
            {
                return new LocationData
                {
                    Country = "N/A",
                    RegionName = "N/A",
                    City = "N/A", 
                    Latitude = 0,
                    Longitude = 0,
                    Timezone = "N/A",
                    ISP = "N/A"
                };
            }
        }

        private string GetLocalIPv4()
        {
            string localIP = "N/A";
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                    break;
                }
            }
            return localIP;
        }

        public string GetPublicIP() // Made public to access from MonitoringForm
        {
            try
            {
                using (var client = new WebClient())
                {
                    // dịch vụ miễn phí: https://api.ipify.org
                    return client.DownloadString("https://api.ipify.org");
                }
            }
            catch
            {
                return "N/A";
            }
        }

        private string GetMacAddress()
        {
            var nic = NetworkInterface
                .GetAllNetworkInterfaces()
                .FirstOrDefault(n => n.OperationalStatus == OperationalStatus.Up);

            return nic?.GetPhysicalAddress().ToString() ?? "N/A";
        }
    }

    public class LocationData
    {
        public string Country { get; set; } = "N/A";
        public string RegionName { get; set; } = "N/A";
        public string City { get; set; } = "N/A";
        public double Latitude { get; set; } = 0;
        public double Longitude { get; set; } = 0;
        public string Timezone { get; set; } = "N/A";
        public string ISP { get; set; } = "N/A";
        
        public string GetCoordinatesString()
        {
            return $"({Latitude:F6}, {Longitude:F6})";
        }
        
        public string GetFullLocationString()
        {
            return $"{City}, {RegionName}, {Country}";
        }
    }
}
