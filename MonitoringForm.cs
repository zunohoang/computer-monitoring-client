using System;
using System.Drawing;
using System.Windows.Forms;
using AntdUI;
using ComputerMonitoringClient.Services;
using ComputerMonitoringClient.Models;
using ComputerMonitoringClient.Utils;

namespace ComputerMonitoringClient.Views
{
    public partial class MonitoringForm : AntdUI.Window
    {
        private readonly AuthenticationService authService;
        private readonly MonitoringService monitoringService;
        private readonly DeviceService deviceService;
        private readonly ProcessService processService; // Add ProcessService
        private ExamSession currentSession;

        private System.Windows.Forms.Panel headerPanel;
        private AntdUI.Label lblHeader;
        private AntdUI.Label lblExamInfo;
        private AntdUI.Label lblRoomInfo;
        private AntdUI.Label lblLocationInfo; // New location label
        private AntdUI.Label lblStatus;
        private AntdUI.Panel contentPanel;
        private RichTextBox txtMonitorLog;
        private AntdUI.Button btnStartMonitoring;
        private AntdUI.Button btnStopMonitoring;
        private AntdUI.Button btnLogout;
        private AntdUI.Button btnReport;
        private AntdUI.Button btnSettings;
        private AntdUI.Button btnAbout;
        private AntdUI.Button btnDeviceInfo;
        private System.Windows.Forms.Timer monitoringTimer;

        public MonitoringForm()
        {
            authService = AuthenticationService.Instance;
            monitoringService = MonitoringService.Instance;
            deviceService = DeviceService.Instance;
            processService = ProcessService.Instance; // Initialize ProcessService
            currentSession = authService.CurrentSession;
            
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Màn hình giám sát";
            this.Size = new Size(Constants.UI.MonitoringFormWidth, Constants.UI.MonitoringFormHeight);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Constants.Colors.Background;

            // Header Panel
            headerPanel = new System.Windows.Forms.Panel
            {
                Location = new Point(0, 0),
                Size = new Size(1000, 120), // Increased height to accommodate location info
                BackColor = Constants.Colors.Primary
            };
            this.Controls.Add(headerPanel);

            // Header Label
            lblHeader = new AntdUI.Label
            {
                Text = "HỆ THỐNG GIÁM SÁT THI",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Constants.Colors.White,
                Location = new Point(20, 10),
                Size = new Size(400, 30)
            };
            headerPanel.Controls.Add(lblHeader);

            // Exam Code Info
            lblExamInfo = new AntdUI.Label
            {
                Text = $"Mã dự thi: {currentSession.ExamCode}",
                Font = new Font("Segoe UI", 10),
                ForeColor = Constants.Colors.White,
                Location = new Point(20, 45),
                Size = new Size(200, 20)
            };
            headerPanel.Controls.Add(lblExamInfo);

            // Room Code Info
            lblRoomInfo = new AntdUI.Label
            {
                Text = $"Phòng thi: {currentSession.RoomCode}",
                Font = new Font("Segoe UI", 10),
                ForeColor = Constants.Colors.White,
                Location = new Point(230, 45),
                Size = new Size(200, 20)
            };
            headerPanel.Controls.Add(lblRoomInfo);

            // Location Info
            lblLocationInfo = new AntdUI.Label
            {
                Text = "Đang lấy vị trí...",
                Font = new Font("Segoe UI", 9),
                ForeColor = Constants.Colors.White,
                Location = new Point(20, 70),
                Size = new Size(400, 40),
                AutoSize = false
            };
            headerPanel.Controls.Add(lblLocationInfo);

            // Device Info Button
            btnDeviceInfo = new AntdUI.Button
            {
                Text = "Thiết bị",
                Location = new Point(480, 40),
                Size = new Size(90, 35),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Type = AntdUI.TTypeMini.Primary, // Use Primary type for better visibility
                BorderWidth = 0f,
                Radius = Constants.UI.DefaultRadius,
                ForeColor = Constants.Colors.White // White text on blue background
            };
            btnDeviceInfo.Click += BtnDeviceInfo_Click;
            headerPanel.Controls.Add(btnDeviceInfo);

            // Settings Button
            btnSettings = new AntdUI.Button
            {
                Text = "Cài đặt",
                Location = new Point(580, 40),
                Size = new Size(80, 35),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Type = AntdUI.TTypeMini.Primary, // Use Primary type for better visibility
                BorderWidth = 0f,
                Radius = Constants.UI.DefaultRadius,
                ForeColor = Constants.Colors.White // White text on blue background
            };
            btnSettings.Click += BtnSettings_Click;
            headerPanel.Controls.Add(btnSettings);

            // Report Button
            btnReport = new AntdUI.Button
            {
                Text = "Báo cáo",
                Location = new Point(670, 40),
                Size = new Size(80, 35),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Type = AntdUI.TTypeMini.Primary, // Use Primary type for better visibility
                BorderWidth = 0f,
                Radius = Constants.UI.DefaultRadius,
                ForeColor = Constants.Colors.White // White text on blue background
            };
            btnReport.Click += BtnReport_Click;
            headerPanel.Controls.Add(btnReport);

            // About Button
            btnAbout = new AntdUI.Button
            {
                Text = "Về",
                Location = new Point(760, 40),
                Size = new Size(50, 35),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Type = AntdUI.TTypeMini.Primary, // Use Primary type for better visibility
                BorderWidth = 0f,
                Radius = Constants.UI.DefaultRadius,
                ForeColor = Constants.Colors.White // White text on blue background
            };
            btnAbout.Click += BtnAbout_Click;
            headerPanel.Controls.Add(btnAbout);

            // Logout Button - Keep red for logout action
            btnLogout = new AntdUI.Button
            {
                Text = "Đăng xuất",
                Location = new Point(820, 40),
                Size = new Size(100, 35),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Type = AntdUI.TTypeMini.Error,
                BorderWidth = 0f,
                Radius = Constants.UI.DefaultRadius,
                ForeColor = Constants.Colors.White // White text on red background
            };
            btnLogout.Click += BtnLogout_Click;
            headerPanel.Controls.Add(btnLogout);

            // Content Panel - Adjusted position for larger header
            contentPanel = new AntdUI.Panel
            {
                Location = new Point(20, 140),
                Size = new Size(950, 510),
                Back = Constants.Colors.White,
                Shadow = Constants.UI.DefaultShadow,
                Radius = Constants.UI.DefaultRadius
            };
            this.Controls.Add(contentPanel);

            // Status Label - Improve text contrast
            lblStatus = new AntdUI.Label
            {
                Text = "Trạng thái: Chưa bắt đầu giám sát",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Constants.Colors.ReadableGray, // Using readable gray color
                Location = new Point(20, 20),
                Size = new Size(500, 30)
            };
            contentPanel.Controls.Add(lblStatus);

            // Monitor Log TextBox - Improve text readability
            txtMonitorLog = new RichTextBox
            {
                Location = new Point(20, 60),
                Size = new Size(910, 360), // Reduced height to fit new layout
                Font = new Font("Consolas", 10),
                BackColor = Constants.Colors.LogBackground, // Using log background color
                ForeColor = Constants.Colors.ReadableText, // Using readable text color
                ReadOnly = true,
                BorderStyle = BorderStyle.FixedSingle
            };
            contentPanel.Controls.Add(txtMonitorLog);

            // Start Monitoring Button
            btnStartMonitoring = new AntdUI.Button
            {
                Text = "Bắt đầu giám sát",
                Location = new Point(20, 440),
                Size = new Size(200, 50),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Type = AntdUI.TTypeMini.Success,
                BorderWidth = 0f,
                Radius = Constants.UI.DefaultRadius,
                ForeColor = Constants.Colors.White // White text on green background
            };
            btnStartMonitoring.Click += BtnStartMonitoring_Click;
            contentPanel.Controls.Add(btnStartMonitoring);

            // Stop Monitoring Button
            btnStopMonitoring = new AntdUI.Button
            {
                Text = "Dừng giám sát",
                Location = new Point(240, 440),
                Size = new Size(200, 50),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Type = AntdUI.TTypeMini.Error,
                BorderWidth = 0f,
                Radius = Constants.UI.DefaultRadius,
                Enabled = false,
                ForeColor = Constants.Colors.White // White text on red background
            };
            btnStopMonitoring.Click += BtnStopMonitoring_Click;
            contentPanel.Controls.Add(btnStopMonitoring);

            // Show Device Info Button (in content panel)
            var btnShowDeviceInfo = new AntdUI.Button
            {
                Text = "Hiển thị thông tin thiết bị",
                Location = new Point(460, 440),
                Size = new Size(160, 50),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Type = AntdUI.TTypeMini.Primary,
                BorderWidth = 0f,
                Radius = Constants.UI.DefaultRadius,
                ForeColor = Constants.Colors.White // White text on blue background
            };
            btnShowDeviceInfo.Click += BtnShowDeviceInfo_Click;
            contentPanel.Controls.Add(btnShowDeviceInfo);

            // Process Info Button - Updated text and functionality
            var btnShowProcessInfo = new AntdUI.Button
            {
                Text = "Giám sát tiến trình",
                Location = new Point(630, 440),
                Size = new Size(140, 50),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Type = AntdUI.TTypeMini.Default,
                BorderWidth = 1f,
                Radius = Constants.UI.DefaultRadius,
                ForeColor = Constants.Colors.Primary
            };
            btnShowProcessInfo.Click += BtnShowProcessInfo_Click;
            contentPanel.Controls.Add(btnShowProcessInfo);

            // Location Coordinates Button 
            var btnShowLocation = new AntdUI.Button
            {
                Text = "Tọa độ vị trí",
                Location = new Point(780, 440),
                Size = new Size(120, 50),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Type = AntdUI.TTypeMini.Default,
                BorderWidth = 1f,
                Radius = Constants.UI.DefaultRadius,
                ForeColor = Constants.Colors.Primary
            };
            btnShowLocation.Click += BtnShowLocation_Click;
            contentPanel.Controls.Add(btnShowLocation);

            // Monitoring Timer
            monitoringTimer = new System.Windows.Forms.Timer();
            monitoringTimer.Interval = Constants.Timer.MonitoringInterval;
            monitoringTimer.Tick += MonitoringTimer_Tick;

            // Add initial logs including device info
            AddLogToUI(new MonitoringLog(LogType.Info, "Hệ thống sẵn sàng."));
            AddLogToUI(new MonitoringLog(LogType.Info, $"Thông tin phiên: {currentSession}"));
            AddDeviceInfoToLog();
            AddLocationInfoToLog();
            AddProcessInfoToLog(); // Add process info to initial logs
        }

        private void BtnStartMonitoring_Click(object sender, EventArgs e)
        {
            monitoringService.StartMonitoring();
            
            btnStartMonitoring.Enabled = false;
            btnStopMonitoring.Enabled = true;
            lblStatus.Text = "Trạng thái: Đang giám sát";
            lblStatus.ForeColor = Constants.Colors.ReadableSuccess; // Using readable success color
            
            monitoringTimer.Start();
            
            AntdUI.Notification.success(this, "Giám sát",
                Constants.Messages.MonitoringStarted,
                AntdUI.TAlignFrom.BR, Font);
        }

        private void BtnStopMonitoring_Click(object sender, EventArgs e)
        {
            monitoringService.StopMonitoring();
            
            btnStartMonitoring.Enabled = true;
            btnStopMonitoring.Enabled = false;
            lblStatus.Text = "Trạng thái: Đã dừng giám sát";
            lblStatus.ForeColor = Constants.Colors.ReadableError; // Using readable error color
            
            monitoringTimer.Stop();
            
            AntdUI.Notification.warn(this, "Giám sát",
                Constants.Messages.MonitoringStopped,
                AntdUI.TAlignFrom.BR, Font);
        }

        private void MonitoringTimer_Tick(object sender, EventArgs e)
        {
            // Sử dụng MonitoringService để thực hiện kiểm tra
            var log = monitoringService.PerformMonitoringCheck();
            if (log != null)
            {
                AddLogToUI(log);
            }
        }

        private void BtnSettings_Click(object sender, EventArgs e)
        {
            // Show settings form with performance test option
            var result = AntdUI.Modal.open(new AntdUI.Modal.Config(this, "Tùy chọn", "Chọn hành động:\n\n1. Mở cài đặt\n2. Kiểm tra hiệu suất hệ thống")
            {
                Icon = AntdUI.TType.Info,
                OkText = "Cài đặt",
                CancelText = "Kiểm tra hiệu suất"
            });

            if (result == DialogResult.OK)
            {
                // Open settings form
                SettingsForm settingsForm = new SettingsForm();
                settingsForm.ShowDialog();
            }
            else if (result == DialogResult.Cancel)
            {
                // Run performance test
                RunPerformanceTest();
            }
        }

        private void RunPerformanceTest()
        {
            try
            {
                // Show loading notification
                AntdUI.Notification.info(this, "Kiểm tra hiệu suất", "Đang chạy test hiệu suất...", AntdUI.TAlignFrom.TR, Font);

                // Run performance test
                var testResults = Utils.PerformanceTestHelper.TestProcessPerformance();
                var memoryResults = Utils.PerformanceTestHelper.TestMemoryUsage();
                var isAcceptable = Utils.PerformanceTestHelper.IsPerformanceAcceptable();

                var fullResults = testResults + "\n\n" + memoryResults;

                // Show results in modal
                var icon = isAcceptable ? AntdUI.TType.Success : AntdUI.TType.Warn;
                var title = isAcceptable ? "Kết quả kiểm tra - TỐT" : "Kết quả kiểm tra - CÓ VẤN ĐỀ";

                AntdUI.Modal.open(new AntdUI.Modal.Config(this, title, fullResults)
                {
                    Icon = icon,
                    OkText = "Đóng",
                    Width = 700
                });

                // Log to monitoring system
                var logType = isAcceptable ? LogType.Success : LogType.Warning;
                AddLogToUI(new MonitoringLog(logType, "Kiểm tra hiệu suất hoàn tất", 
                    $"Hiệu suất hệ thống: {(isAcceptable ? "Tốt" : "Cần cải thiện")}"));
            }
            catch (Exception ex)
            {
                AntdUI.Modal.open(new AntdUI.Modal.Config(this, "Lỗi", $"Không thể chạy test hiệu suất: {ex.Message}")
                {
                    Icon = AntdUI.TType.Error,
                    OkText = "Đóng"
                });
            }
        }

        private void BtnReport_Click(object sender, EventArgs e)
        {
            ReportForm reportForm = new ReportForm();
            reportForm.ShowDialog();
        }

        private void BtnAbout_Click(object sender, EventArgs e)
        {
            AboutForm aboutForm = new AboutForm();
            aboutForm.ShowDialog();
        }

        private void BtnLogout_Click(object sender, EventArgs e)
        {
            var result = AntdUI.Modal.open(new AntdUI.Modal.Config(this, "Xác nhận", Constants.Messages.LogoutConfirm)
            {
                Icon = AntdUI.TType.Warn,
                OkText = "Đăng xuất",
                CancelText = "Hủy"
            });

            if (result == DialogResult.OK)
            {
                if (monitoringService.IsMonitoring)
                {
                    monitoringTimer.Stop();
                    monitoringService.StopMonitoring();
                }
                
                authService.Logout();
                this.Close();
            }
        }

        private void BtnDeviceInfo_Click(object sender, EventArgs e)
        {
            ShowDeviceInfoDialog();
        }

        private void BtnShowDeviceInfo_Click(object sender, EventArgs e)
        {
            AddDeviceInfoToLog();
        }

        private void ShowDeviceInfoDialog()
        {
            string deviceInfo = deviceService.GetDeviceInfo();
            
            AntdUI.Modal.open(new AntdUI.Modal.Config(this, "Thông tin thiết bị", deviceInfo)
            {
                Icon = AntdUI.TType.Info,
                OkText = "Đóng",
                Width = 500
            });
        }

        private void AddDeviceInfoToLog()
        {
            try
            {
                string deviceInfo = deviceService.GetDeviceInfo();
                AddLogToUI(new MonitoringLog(LogType.Info, "Thông tin thiết bị:", deviceInfo.Trim()));
            }
            catch (Exception ex)
            {
                AddLogToUI(new MonitoringLog(LogType.Error, "Lỗi khi lấy thông tin thiết bị", ex.Message));
            }
        }

        private void BtnShowProcessInfo_Click(object sender, EventArgs e)
        {
            // Open dedicated Process Monitoring Form
            ProcessMonitoringForm processForm = new ProcessMonitoringForm();
            processForm.ShowDialog();
        }

        private void AddProcessInfoToLog()
        {
            try
            {
                var processOverview = monitoringService.GetProcessOverview();
                AddLogToUI(new MonitoringLog(LogType.Info, "Tổng quan tiến trình:", processOverview));

                // Kiểm tra tiến trình đáng ngờ
                var suspiciousProcesses = monitoringService.GetCurrentSuspiciousProcesses();
                if (suspiciousProcesses.Count > 0)
                {
                    var suspiciousNames = string.Join(", ", suspiciousProcesses.ConvertAll(p => p.Name));
                    AddLogToUI(new MonitoringLog(LogType.Warning, $"Phát hiện {suspiciousProcesses.Count} tiến trình đáng ngờ:", suspiciousNames));
                }
                else
                {
                    AddLogToUI(new MonitoringLog(LogType.Success, "Không phát hiện tiến trình đáng ngờ"));
                }
            }
            catch (Exception ex)
            {
                AddLogToUI(new MonitoringLog(LogType.Error, "Lỗi khi kiểm tra tiến trình", ex.Message));
            }
        }

        /// <summary>
        /// Force refresh process cache for immediate updates
        /// </summary>
        private void RefreshProcessCache()
        {
            try
            {
                processService.RefreshCache();
                AddLogToUI(new MonitoringLog(LogType.Info, "Đã làm mới bộ nhớ cache tiến trình"));
            }
            catch (Exception ex)
            {
                AddLogToUI(new MonitoringLog(LogType.Error, "Lỗi khi làm mới cache tiến trình", ex.Message));
            }
        }

        private void BtnShowLocation_Click(object sender, EventArgs e)
        {
            ShowLocationCoordinatesDialog();
        }

        private async void LoadLocationInfoAsync()
        {
            try
            {
                // Run location loading in background to avoid blocking UI
                await System.Threading.Tasks.Task.Run(() =>
                {
                    var locationData = deviceService.GetDetailedLocationInfo();
                    
                    // Update UI on main thread
                    this.Invoke(new Action(() =>
                    {
                        if (locationData != null)
                        {
                            lblLocationInfo.Text = $"Vị trí: {locationData.GetFullLocationString()} | Tọa độ: {locationData.GetCoordinatesString()}";
                        }
                        else
                        {
                            lblLocationInfo.Text = "Không thể xác định vị trí";
                        }
                    }));
                });
            }
            catch (Exception ex)
            {
                lblLocationInfo.Text = $"Lỗi vị trí: {ex.Message}";
            }
        }

        private void ShowLocationCoordinatesDialog()
        {
            try
            {
                var locationData = deviceService.GetDetailedLocationInfo();
                
                if (locationData != null)
                {
                    string locationDetails = $@"THÔM TIN VỊ TRÍ CHI TIẾT

🌍 Địa điểm:
   • Quốc gia: {locationData.Country}
   • Tỉnh/Thành phố: {locationData.RegionName}
   • Thành phố: {locationData.City}

📍 Tọa độ GPS:
   • Vĩ độ (Latitude): {locationData.Latitude:F6}°
   • Kinh độ (Longitude): {locationData.Longitude:F6}°
   • Tọa độ đầy đủ: {locationData.GetCoordinatesString()}

🕐 Múi giờ: {locationData.Timezone}

🌐 Nhà mạng: {locationData.ISP}

📊 IP Public: {deviceService.GetPublicIP()}";


                    AntdUI.Modal.open(new AntdUI.Modal.Config(this, "Tọa độ vị trí", locationDetails)
                    {
                        Icon = AntdUI.TType.Info,
                        OkText = "Đóng",
                        Width = 600
                    });
                }
                else
                {
                    AntdUI.Modal.open(new AntdUI.Modal.Config(this, "Thông báo", "Không thể lấy thông tin vị trí hiện tại.")
                    {
                        Icon = AntdUI.TType.Warn,
                        OkText = "Đóng"
                    });
                }
            }
            catch (Exception ex)
            {
                AntdUI.Modal.open(new AntdUI.Modal.Config(this, "Lỗi", $"Lỗi khi lấy thông tin vị trí: {ex.Message}")
                {
                    Icon = AntdUI.TType.Error,
                    OkText = "Đóng"
                });
            }
        }

        private void AddLocationInfoToLog()
        {
            try
            {
                var locationData = deviceService.GetDetailedLocationInfo();
                if (locationData != null)
                {
                    string locationLog = $"Vị trí: {locationData.GetFullLocationString()} - Tọa độ: {locationData.GetCoordinatesString()}";
                    AddLogToUI(new MonitoringLog(LogType.Info, "Thông tin vị trí:", locationLog));
                }
                else
                {
                    AddLogToUI(new MonitoringLog(LogType.Warning, "Không thể xác định vị trí thiết bị"));
                }
            }
            catch (Exception ex)
            {
                AddLogToUI(new MonitoringLog(LogType.Error, "Lỗi khi lấy thông tin vị trí", ex.Message));
            }
        }

        private void AddLogToUI(MonitoringLog log)
        {
            string logEntry = $"{log}\n";

            txtMonitorLog.SelectionStart = txtMonitorLog.TextLength;
            txtMonitorLog.SelectionLength = 0;

            switch (log.Type)
            {
                case LogType.Warning:
                    txtMonitorLog.SelectionColor = Constants.Colors.ReadableWarning; // Using readable warning color
                    break;
                case LogType.Error:
                    txtMonitorLog.SelectionColor = Constants.Colors.ReadableError; // Using readable error color
                    break;
                case LogType.Success:
                    txtMonitorLog.SelectionColor = Constants.Colors.ReadableSuccess; // Using readable success color
                    break;
                default:
                    txtMonitorLog.SelectionColor = Constants.Colors.ReadableInfo; // Using readable info color
                    break;
            }

            txtMonitorLog.AppendText(logEntry);
            txtMonitorLog.ScrollToCaret();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (monitoringTimer != null)
            {
                monitoringTimer.Stop();
                monitoringTimer.Dispose();
            }
            base.OnFormClosing(e);
        }
    }
}
