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
        public static string? CurrentToken { get; set; }
        public static long? CurrentAttemptId { get; set; }
        public static long? CurrentContestId { get; set; }
        public static long? CurrentUserId { get; set; }
        public static long? CurrentRoomName { get; set; }
        public static User? CurrentUser { get; set; }
    }
}
