using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net.Http;
using Newtonsoft.Json;
using ComputerMonitoringClient.Dtos;

namespace ComputerMonitoringClient.Services
{
    public class DeviceService
    {
        public static DeviceService Instance { get; } = new DeviceService();

        public DeviceService() { }

        public async Task<DeviceDetailDto> GetDeviceDetailAsync()
        {
            var deviceName = Environment.MachineName;
            var osVersion = Environment.OSVersion.ToString();
            var userName = Environment.UserName;
            var architecture = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString();
            
            var ipAddress = await GetPublicIPAddressAsync();
            var locationInfo = await GetLocationFromIPAsync(ipAddress);

            return new DeviceDetailDto
            {
                DeviceName = deviceName,
                OSVersion = osVersion,
                UserName = userName,
                Architecture = architecture,
                IPAddress = ipAddress,
                Latitude = locationInfo.Latitude,
                Longitude = locationInfo.Longitude,
                Location = $"{locationInfo.Latitude}, {locationInfo.Longitude} - {locationInfo.Location}"
            };
        }

        // public async Task<DeviceDetailDto> GetDeviceDetailAsync()
        // {
        //     var deviceName = Environment.MachineName;
        //     var osVersion = Environment.OSVersion.ToString();
        //     var userName = Environment.UserName;
        //     var architecture = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString();
        //     var ipAddress = GetLocalIPAddress();

        //     return new DeviceDetailDto
        //     {
        //         DeviceName = deviceName,
        //         OSVersion = osVersion,
        //         UserName = userName,
        //         Architecture = architecture,
        //         IPAddress = ipAddress,
        //         Latitude = 0.0,
        //         Longitude = 0.0,
        //         Location = "Unknown"
        //     };
        // }

        //  /// <summary>
        /// Lấy địa chỉ IP public (global) của máy.
        /// </summary>
        /// <returns>Địa chỉ IP public dưới dạng string</returns>
        public async Task<string> GetPublicIPAddressAsync()
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(10);

                    // API miễn phí để lấy IP public
                    string ip = await httpClient.GetStringAsync("https://api.ipify.org");
                    return ip.Trim();
                }
            }
            catch
            {
                return "127.0.0.1";
            }
        }


        /// <summary>
        /// Lấy thông tin vị trí địa lý từ địa chỉ IP
        /// </summary>
        /// <param name="ipAddress">Địa chỉ IP</param>
        /// <returns>Thông tin vị trí bao gồm kinh độ, vĩ độ và địa chỉ</returns>
        public async Task<LocationInfo> GetLocationFromIPAsync(string ipAddress)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(10);
                    
                    // Sử dụng API miễn phí ip-api.com
                    var response = await httpClient.GetStringAsync($"http://ip-api.com/json/{ipAddress}");
                    var locationData = JsonConvert.DeserializeObject<IpApiResponse>(response);

                    if (locationData != null && locationData.Status == "success")
                    {
                        return new LocationInfo
                        {
                            Latitude = locationData.Lat,
                            Longitude = locationData.Lon,
                            Location = $"{locationData.City}, {locationData.RegionName}, {locationData.Country}"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting location: {ex.Message}");
            }

            return new LocationInfo
            {
                Latitude = 0.0,
                Longitude = 0.0,
                Location = "Unknown"
            };
        }

        // Helper classes for JSON deserialization
        private class IpApiResponse
        {
            public string Status { get; set; }
            public string Country { get; set; }
            public string RegionName { get; set; }
            public string City { get; set; }
            public double Lat { get; set; }
            public double Lon { get; set; }
        }

        public class LocationInfo
        {
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public string Location { get; set; }
        }
    }
}
