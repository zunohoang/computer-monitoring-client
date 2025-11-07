using Microsoft.VisualBasic.ApplicationServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComputerMonitoringClient.Networks
{
    public static class AppHttpSession
    {
        public static string? Token { get; set; }
        public static User? CurrentUser { get; set; }
    }
}
