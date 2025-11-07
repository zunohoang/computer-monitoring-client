using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComputerMonitoringClient.Utils
{
    public static class LoggerProvider
    {
        public static readonly ILoggerFactory Factory = LoggerFactory.Create(builder =>
        {
            builder
                .AddConsole() // hiện log trong terminal nếu chạy dotnet run
                .AddDebug();  // hiện log trong Visual Studio → Output → Debug
        });

        public static ILogger<T> CreateLogger<T>()
        {
            return Factory.CreateLogger<T>();
        }
    }
}
