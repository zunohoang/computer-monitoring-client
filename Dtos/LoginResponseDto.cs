using Microsoft.VisualBasic.ApplicationServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComputerMonitoringClient.Dtos
{
    public class UserDetails
    {
        public string? id { get; set; }
        public string? name { get; set; }
        public string? username { get; set; }
        public string? email { get; set; }
        public string? role { get; set; }
    }
    public class LoginResponse
    {
        public bool? success { get; set; }
        public string? message { get; set; }
        public string? token { get; set; }
        public UserDetails? user { get; set; }
    }
}
