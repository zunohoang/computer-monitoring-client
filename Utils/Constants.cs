using System.Drawing;

namespace ComputerMonitoringClient.Utils
{
    /// <summary>
    /// Class chứa các hằng số được sử dụng trong ứng dụng
    /// </summary>
    public static class Constants
    {
        // Colors theo Ant Design
        public static class Colors
        {
            public static readonly Color Primary = Color.FromArgb(24, 144, 255);
            public static readonly Color Success = Color.FromArgb(82, 196, 26);
            public static readonly Color Warning = Color.FromArgb(250, 140, 22);
            public static readonly Color Error = Color.FromArgb(255, 77, 79);
            public static readonly Color Info = Color.FromArgb(24, 144, 255);
            public static readonly Color Background = Color.FromArgb(240, 242, 245);
            public static readonly Color White = Color.White;
            public static readonly Color TextPrimary = Color.FromArgb(217, 0, 0, 0);
            public static readonly Color TextSecondary = Color.FromArgb(140, 140, 140);
            
            // Readable colors for better contrast
            public static readonly Color ReadableSuccess = Color.FromArgb(22, 119, 22);
            public static readonly Color ReadableWarning = Color.FromArgb(181, 107, 0);
            public static readonly Color ReadableError = Color.FromArgb(183, 28, 28);
            public static readonly Color ReadableInfo = Color.FromArgb(25, 118, 210);
            public static readonly Color ReadableText = Color.FromArgb(33, 37, 41);
            public static readonly Color ReadableGray = Color.FromArgb(120, 120, 120);
            public static readonly Color LogBackground = Color.FromArgb(248, 249, 250);
        }

        // Messages
        public static class Messages
        {
            public const string LoginSuccess = "Đăng nhập thành công!";
            public const string LogoutConfirm = "Bạn có chắc chắn muốn đăng xuất?";
            public const string ExamCodeRequired = "Vui lòng nhập mã dự thi!";
            public const string RoomCodeRequired = "Vui lòng nhập mã phòng thi!";
            public const string MonitoringStarted = "Đã bắt đầu giám sát hệ thống";
            public const string MonitoringStopped = "Đã dừng giám sát hệ thống";
        }

        // UI Settings
        public static class UI
        {
            public const int LoginFormWidth = 520;
            public const int LoginFormHeight = 450;
            public const int MonitoringFormWidth = 1000;
            public const int MonitoringFormHeight = 700;
            public const int DefaultRadius = 6;
            public const int DefaultShadow = 10;
        }

        // Timer Settings
        public static class Timer
        {
            public const int MonitoringInterval = 1000; // milliseconds
        }
    }
}
