using ComputerMonitoringClient.Dtos;
using ComputerMonitoringClient.Networks;
using ComputerMonitoringClient.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComputerMonitoringClient.Services.Auth
{
    public class AuthService
    {
        private readonly ILogger<AuthService> _logger;

        public AuthService()
        {
            _logger = LoggerProvider.CreateLogger<AuthService>();
        }
        public async Task<LoginResponse?> LoginAsync(LoginRequest req)
        {
            _logger?.LogInformation("Attempting to log in user with email: {Email} - password: {Password}", req.email, req.password);
            LoginResponse loginResponse = await ApiClient.Instance.PostAsync<LoginResponse>("Auth/login", req);

            if (loginResponse?.success == true && !string.IsNullOrEmpty(loginResponse.token))
            {
                AppHttpSession.Token = loginResponse.token;
                // Properties.Settings.Default.AuthToken = loginResponse.token;
                // Properties.Settings.Default.UserEmail = loginResponse.user?.email ?? "";
                // Properties.Settings.Default.FullName  = loginResponse.user?.name ?? "";
                // Properties.Settings.Default.Save(); // lưu vào local
                _logger?.LogInformation("Token saved locally in user settings.");
            }

            return loginResponse;
        }
    }
}
