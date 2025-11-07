using ComputerMonitoringClient.Utils;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ComputerMonitoringClient.Networks
{
    public sealed class ApiClient
    {
        private static readonly Lazy<ApiClient> _instance =
            new Lazy<ApiClient>(() => new ApiClient());

        private readonly HttpClient _client;

        private readonly string BASE_URL = Environment.GetEnvironmentVariable("MoniTest_BACKEND_URL") ?? "http://localhost:5045/api/";
        private const int TIMEOUT_SECONDS = 30;

        public static ApiClient Instance => _instance.Value;

        private readonly ILogger<ApiClient> _logger =
            LoggerProvider.CreateLogger<ApiClient>();

        private ApiClient()
        {
            _client = new HttpClient
            {
                BaseAddress = new Uri(BASE_URL)
            };
            _client.Timeout = TimeSpan.FromSeconds(TIMEOUT_SECONDS);
        }

        private void AttachToken()
        {
            if (!string.IsNullOrEmpty(AppHttpSession.Token))
            {
                _client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", AppHttpSession.Token);
            }
            //else if (Properties.Settings.Default.AuthToken != "")
            //{
            //    AppHttpSession.Token = Properties.Settings.Default.AuthToken;
            //    _client.DefaultRequestHeaders.Authorization =
            //        new AuthenticationHeaderValue("Bearer", AppHttpSession.Token);
            //}
        }

        public async Task<T?> GetAsync<T>(string endpoint)
        {
            AttachToken();
            var response = await _client.GetAsync(endpoint);
            return await HandleResponse<T>(response);
        }

        public async Task<T?> PostAsync<T>(string endpoint, object data)
        {
            AttachToken();
            var json = JsonConvert.SerializeObject(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            _logger.LogInformation("POST {Endpoint} with payload: {Payload}", endpoint, json);
            var response = await _client.PostAsync(endpoint, content);
            _logger.LogInformation("Response Status: {StatusCode}", response.StatusCode);
            return await HandleResponse<T>(response);
        }

        private async Task<T?> HandleResponse<T>(HttpResponseMessage response)
        {
            string body = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
                return JsonConvert.DeserializeObject<T>(body);

            Console.WriteLine($"[API ERROR] {response.StatusCode}: {body}");
            return default;
        }
    }
}
