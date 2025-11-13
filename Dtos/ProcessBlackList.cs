using Microsoft.VisualBasic.ApplicationServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComputerMonitoringClient.Dtos
{
    public class ProcessBlackList
    {
        public List<string> processNames { get; set; } = new List<string>();
    }
}
