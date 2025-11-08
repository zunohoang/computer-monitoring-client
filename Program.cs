using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using ComputerMonitoringClient.Views;

namespace ComputerMonitoringClient
{
    internal static class Program
    {
        // Import để show console window
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Show console window for debugging
            #if DEBUG
            AllocConsole();
            Console.WriteLine("=== Computer Monitoring Client Debug Console ===");
            Console.WriteLine($"Started at: {DateTime.Now}");
            Console.WriteLine();
            #endif

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new LoginForm());
        }
    }
}
