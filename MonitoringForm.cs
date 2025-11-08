using System;
using System.Drawing;
using System.Windows.Forms;
using AntdUI;
using ComputerMonitoringClient.Services;
using ComputerMonitoringClient.Models;
using ComputerMonitoringClient.Utils;
using Microsoft.Extensions.Logging;

namespace ComputerMonitoringClient.Views
{
    public partial class MonitoringForm : AntdUI.Window
    {
        private readonly AuthenticationService authService;
        private readonly MonitoringService monitoringService;
        private readonly DeviceService deviceService;
        private readonly ProcessService processService; // Add ProcessService
        private readonly MonitoringHubClient hubClient; // Add SignalR hub client
        private ExamSession currentSession;

        // Track previous process state for detecting changes
        private Dictionary<int, string> previousProcessStatuses = new Dictionary<int, string>();
        private HashSet<int> previousProcessPids = new HashSet<int>();

        private System.Windows.Forms.Panel headerPanel = null!;
        private AntdUI.Label lblHeader = null!;
        private AntdUI.Label lblExamInfo = null!;
        private AntdUI.Label lblRoomInfo = null!;
        private AntdUI.Label lblLocationInfo = null!; // New location label
        private AntdUI.Label lblStatus = null!;
        private AntdUI.Panel contentPanel = null!;
        private RichTextBox txtMonitorLog = null!;
        private AntdUI.Button btnStartMonitoring = null!;
        private AntdUI.Button btnStopMonitoring = null!;
        private AntdUI.Button btnLogout = null!;
        private AntdUI.Button btnReport = null!;
        private AntdUI.Button btnSettings = null!;
        private AntdUI.Button btnAbout = null!;
        private AntdUI.Button btnDeviceInfo = null!;
        private System.Windows.Forms.Timer monitoringTimer = null!;
        private System.Windows.Forms.Timer dataUploadTimer = null!; // Timer for uploading data to server

        public MonitoringForm()
        {
            authService = AuthenticationService.Instance;
            monitoringService = MonitoringService.Instance;
            deviceService = DeviceService.Instance;
            processService = ProcessService.Instance; // Initialize ProcessService
            hubClient = MonitoringHubClient.Instance; // Get SignalR hub client instance
            currentSession = authService.CurrentSession;
            
            InitializeComponent();
            SetupSignalREvents(); // Setup SignalR event handlers
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

            // Data Upload Timer - Send data to server every 30 seconds
            dataUploadTimer = new System.Windows.Forms.Timer();
            dataUploadTimer.Interval = 30000; // 30 seconds
            dataUploadTimer.Tick += DataUploadTimer_Tick;
            dataUploadTimer.Start(); // Start automatically when form loads

            // Add initial logs including device info
            AddLogToUI(new MonitoringLog(LogType.Info, "Hệ thống sẵn sàng."));
            AddLogToUI(new MonitoringLog(LogType.Info, $"Thông tin phiên: {currentSession}"));
            
            // Load device and location info asynchronously to avoid blocking UI
            LoadDeviceInfoAsync();
            LoadLocationInfoAsync();
            
            // Comment out process info to avoid lag on startup
            // AddProcessInfoToLog(); // Add process info to initial logs
            
            // Register attempt with SignalR
            RegisterAttemptWithServer();
        }

        private void BtnStartMonitoring_Click(object? sender, EventArgs e)
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

        private void BtnStopMonitoring_Click(object? sender, EventArgs e)
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

        private void MonitoringTimer_Tick(object? sender, EventArgs e)
        {
            // Sử dụng MonitoringService để thực hiện kiểm tra
            var log = monitoringService.PerformMonitoringCheck();
            if (log != null)
            {
                AddLogToUI(log);
            }
        }

        private void BtnSettings_Click(object? sender, EventArgs e)
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

        private void BtnReport_Click(object? sender, EventArgs e)
        {
            ReportForm reportForm = new ReportForm();
            reportForm.ShowDialog();
        }

        private void BtnAbout_Click(object? sender, EventArgs e)
        {
            AboutForm aboutForm = new AboutForm();
            aboutForm.ShowDialog();
        }

        private void BtnLogout_Click(object? sender, EventArgs e)
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

        private void BtnDeviceInfo_Click(object? sender, EventArgs e)
        {
            ShowDeviceInfoDialog();
        }

        private void BtnShowDeviceInfo_Click(object? sender, EventArgs e)
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

        private void BtnShowProcessInfo_Click(object? sender, EventArgs e)
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

        private void BtnShowLocation_Click(object? sender, EventArgs e)
        {
            ShowLocationCoordinatesDialog();
        }

        private async void LoadDeviceInfoAsync()
        {
            try
            {
                // Run device info loading in background to avoid blocking UI
                await System.Threading.Tasks.Task.Run(() =>
                {
                    string deviceInfo = deviceService.GetDeviceInfo();
                    
                    // Update UI on main thread
                    this.Invoke(new Action(() =>
                    {
                        AddLogToUI(new MonitoringLog(LogType.Info, "Thông tin thiết bị:", deviceInfo.Trim()));
                    }));
                });
            }
            catch (Exception ex)
            {
                AddLogToUI(new MonitoringLog(LogType.Error, "Lỗi khi lấy thông tin thiết bị", ex.Message));
            }
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
                            
                            // Also add to log
                            string locationLog = $"Vị trí: {locationData.GetFullLocationString()} - Tọa độ: {locationData.GetCoordinatesString()}";
                            AddLogToUI(new MonitoringLog(LogType.Info, "Thông tin vị trí:", locationLog));
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
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() =>
                    {
                        lblLocationInfo.Text = $"Lỗi vị trí: {ex.Message}";
                    }));
                }
                else
                {
                    lblLocationInfo.Text = $"Lỗi vị trí: {ex.Message}";
                }
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

        /// <summary>
        /// Setup SignalR event handlers to receive status updates from server
        /// </summary>
        private void SetupSignalREvents()
        {
            if (hubClient == null)
            {
                AddLogToUI(new MonitoringLog(LogType.Warning, "SignalR hub client không khả dụng"));
                return;
            }

            // Subscribe to attempt status updates
            hubClient.OnAttemptStatusUpdated += HandleAttemptStatusUpdate;
            hubClient.OnStatusUpdated += HandleSimpleStatusUpdate;
            hubClient.OnError += HandleSignalRError;
            hubClient.OnDisconnected += HandleSignalRDisconnected;

            AddLogToUI(new MonitoringLog(LogType.Info, "Đã đăng ký nhận cập nhật trạng thái từ server"));
        }

        /// <summary>
        /// Handle detailed attempt status updates from SignalR
        /// </summary>
        private void HandleAttemptStatusUpdate(Dtos.AttemptStatusUpdateDto update)
        {
            // Make sure we update UI on the UI thread
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => HandleAttemptStatusUpdate(update)));
                return;
            }

            AddLogToUI(new MonitoringLog(LogType.Info, 
                $"Cập nhật trạng thái: {update.status}", 
                $"AttemptId: {update.attemptId}, Thời gian: {update.timestamp.ToLocalTime():HH:mm:ss}"));

            // Update status based on received status
            switch (update.status.ToLower())
            {
                case "started":
                    lblStatus.Text = "Trạng thái: Đã bắt đầu thi";
                    lblStatus.ForeColor = Constants.Colors.ReadableSuccess;
                    
                    AntdUI.Notification.info(this, "Bắt đầu thi",
                        $"Bài thi đã được bắt đầu lúc {update.timestamp.ToLocalTime():HH:mm:ss}",
                        AntdUI.TAlignFrom.BR, Font);
                    break;

                case "ended":
                    lblStatus.Text = "Trạng thái: Đã kết thúc thi";
                    lblStatus.ForeColor = Constants.Colors.ReadableError;
                    
                    // Stop monitoring if running
                    if (monitoringService.IsMonitoring)
                    {
                        monitoringService.StopMonitoring();
                        monitoringTimer.Stop();
                        btnStartMonitoring.Enabled = false;
                        btnStopMonitoring.Enabled = false;
                    }
                    
                    AntdUI.Notification.warn(this, "Kết thúc thi",
                        $"Bài thi đã kết thúc lúc {update.timestamp.ToLocalTime():HH:mm:ss}",
                        AntdUI.TAlignFrom.BR, Font);
                    break;

                case "approved":
                    lblStatus.Text = "Trạng thái: Đã phê duyệt";
                    lblStatus.ForeColor = Constants.Colors.ReadableSuccess;
                    break;

                case "rejected":
                    lblStatus.Text = "Trạng thái: Bị từ chối";
                    lblStatus.ForeColor = Constants.Colors.ReadableError;
                    
                    AntdUI.Modal.open(new AntdUI.Modal.Config(this, "Bị từ chối",
                        "Bạn đã bị từ chối tham gia bài thi. Vui lòng liên hệ giám thị.")
                    {
                        Icon = AntdUI.TType.Error,
                        OkText = "Đóng",
                        OnOk = (config) =>
                        {
                            this.Close();
                            return true;
                        }
                    });
                    break;

                case "pending":
                    lblStatus.Text = "Trạng thái: Đang chờ phê duyệt";
                    lblStatus.ForeColor = Constants.Colors.Warning;
                    break;

                default:
                    lblStatus.Text = $"Trạng thái: {update.status}";
                    lblStatus.ForeColor = Constants.Colors.ReadableGray;
                    break;
            }
        }

        /// <summary>
        /// Handle simple status updates (backward compatible)
        /// </summary>
        private void HandleSimpleStatusUpdate(string status)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => HandleSimpleStatusUpdate(status)));
                return;
            }

            AddLogToUI(new MonitoringLog(LogType.Info, $"Cập nhật trạng thái đơn giản: {status}"));
        }

        /// <summary>
        /// Handle SignalR errors
        /// </summary>
        private void HandleSignalRError(string error)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => HandleSignalRError(error)));
                return;
            }

            AddLogToUI(new MonitoringLog(LogType.Error, "Lỗi SignalR", error));
        }

        /// <summary>
        /// Handle SignalR disconnection
        /// </summary>
        private void HandleSignalRDisconnected(Exception ex)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => HandleSignalRDisconnected(ex)));
                return;
            }

            AddLogToUI(new MonitoringLog(LogType.Warning, 
                "Mất kết nối SignalR", 
                ex?.Message ?? "Kết nối bị ngắt"));
        }

        /// <summary>
        /// Cleanup SignalR event handlers
        /// </summary>
        private void CleanupSignalREvents()
        {
            if (hubClient != null)
            {
                hubClient.OnAttemptStatusUpdated -= HandleAttemptStatusUpdate;
                hubClient.OnStatusUpdated -= HandleSimpleStatusUpdate;
                hubClient.OnError -= HandleSignalRError;
                hubClient.OnDisconnected -= HandleSignalRDisconnected;
                
                AddLogToUI(new MonitoringLog(LogType.Info, "Đã hủy đăng ký nhận cập nhật từ server"));
            }
        }

        /// <summary>
        /// Register attempt with server via SignalR
        /// </summary>
        private async void RegisterAttemptWithServer()
        {
            try
            {
                Console.WriteLine($"[RegisterAttempt] Starting registration - AttemptId: {hubClient?.CurrentAttemptId}, IsConnected: {hubClient?.IsConnected}");
                
                if (hubClient?.CurrentAttemptId == null)
                {
                    Console.WriteLine("[RegisterAttempt] ERROR: No attemptId to register");
                    AddLogToUI(new MonitoringLog(LogType.Warning, "Không có attemptId để đăng ký"));
                    return;
                }

                var deviceId = Environment.MachineName;
                var deviceName = deviceService.GetDeviceInfo().Split('\n')[0]; // First line usually has device name
                var ipAddress = deviceService.GetPublicIP();

                Console.WriteLine($"[RegisterAttempt] Registering with - DeviceId: {deviceId}, IP: {ipAddress}");
                
                await hubClient.RegisterAttemptAsync(
                    hubClient.CurrentAttemptId.Value,
                    deviceId,
                    deviceName,
                    ipAddress
                );

                Console.WriteLine("[RegisterAttempt] Registration successful!");
                AddLogToUI(new MonitoringLog(LogType.Success, 
                    "Đã đăng ký thiết bị với server", 
                    $"AttemptId: {hubClient.CurrentAttemptId}, Device: {deviceId}"));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RegisterAttempt] ERROR: {ex.Message}");
                Console.WriteLine($"[RegisterAttempt] Stack trace: {ex.StackTrace}");
                AddLogToUI(new MonitoringLog(LogType.Error, "Lỗi khi đăng ký với server", ex.Message));
            }
        }

        /// <summary>
        /// Timer tick event to upload data to server periodically
        /// </summary>
        private async void DataUploadTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                Console.WriteLine($"[DataUploadTimer] Timer tick - IsConnected: {hubClient?.IsConnected}, AttemptId: {hubClient?.CurrentAttemptId}");
                
                if (hubClient?.CurrentAttemptId == null || !hubClient.IsConnected)
                {
                    Console.WriteLine("[DataUploadTimer] Skipping - Not connected or no attempt ID");
                    _logger?.LogWarning("Skipping data upload - Not connected or no attempt ID");
                    return; // Skip if not connected
                }

                var attemptId = hubClient.CurrentAttemptId.Value;
                Console.WriteLine($"[DataUploadTimer] Starting data upload for attempt {attemptId}");

                // Collect system telemetry
                var telemetry = new
                {
                    timestamp = DateTime.UtcNow,
                    systemInfo = processService.GetSystemResourceInfo(),
                    processCount = processService.GetProcessCount(),
                    isMonitoring = monitoringService.IsMonitoring
                };

                // Send telemetry
                Console.WriteLine("[DataUploadTimer] Sending telemetry...");
                await hubClient.SendTelemetryAsync(attemptId, telemetry);
                Console.WriteLine("[DataUploadTimer] Telemetry sent successfully");

                // Get current processes
                var processes = GetCurrentProcesses();
                Console.WriteLine($"[DataUploadTimer] Got {processes.Count} current processes");
                
                if (processes.Count > 0)
                {
                    // Check for process changes (new, ended, or status changed)
                    var changedProcesses = DetectProcessChanges(processes);
                    Console.WriteLine($"[DataUploadTimer] Detected {changedProcesses.Count} changed processes");
                    
                    // If there are changes, send immediately
                    if (changedProcesses.Count > 0)
                    {
                        Console.WriteLine($"[DataUploadTimer] Sending {changedProcesses.Count} changed processes immediately...");
                        await hubClient.SendProcessListAsync(attemptId, changedProcesses);
                        Console.WriteLine($"[DataUploadTimer] Changed processes sent successfully");
                        _logger?.LogInformation($"Sent {changedProcesses.Count} changed processes immediately");
                    }
                    
                    // Also send full list periodically (every 30s via timer)
                    Console.WriteLine($"[DataUploadTimer] Sending full process list ({processes.Count} processes)...");
                    await hubClient.SendProcessListAsync(attemptId, processes);
                    Console.WriteLine("[DataUploadTimer] Full process list sent successfully");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DataUploadTimer] ERROR: {ex.Message}");
                Console.WriteLine($"[DataUploadTimer] Stack trace: {ex.StackTrace}");
                _logger?.LogError(ex, "Error in DataUploadTimer_Tick");
            }
        }

        /// <summary>
        /// Detect process changes (new processes, ended processes, status changes)
        /// </summary>
        private List<object> DetectProcessChanges(List<object> currentProcesses)
        {
            var changedProcesses = new List<object>();
            var currentPids = new HashSet<int>();
            var currentStatuses = new Dictionary<int, string>();

            // Extract current process info
            foreach (dynamic proc in currentProcesses)
            {
                int pid = proc.pid;
                string status = proc.status ?? "Unknown";
                currentPids.Add(pid);
                currentStatuses[pid] = status;

                // Check for new process
                if (!previousProcessPids.Contains(pid))
                {
                    changedProcesses.Add(proc);
                    _logger?.LogInformation($"New process detected: PID={pid}, Name={proc.name}");
                }
                // Check for status change
                else if (previousProcessStatuses.TryGetValue(pid, out var prevStatus) && prevStatus != status)
                {
                    changedProcesses.Add(proc);
                    _logger?.LogInformation($"Process status changed: PID={pid}, Name={proc.name}, {prevStatus} -> {status}");
                }
            }

            // Check for ended processes
            foreach (var prevPid in previousProcessPids)
            {
                if (!currentPids.Contains(prevPid))
                {
                    // Create a "stopped" entry for ended process
                    var endedProcess = new
                    {
                        pid = prevPid,
                        parentPid = (int?)null,
                        name = "Unknown",
                        description = "Process ended",
                        memoryUsage = 0L,
                        threadCount = 0,
                        isSuspicious = false,
                        windowTitle = "",
                        filePath = "",
                        status = "Stopped",
                        startTime = DateTime.MinValue
                    };
                    changedProcesses.Add(endedProcess);
                    _logger?.LogInformation($"Process ended: PID={prevPid}");
                }
            }

            // Update tracking state
            previousProcessPids = currentPids;
            previousProcessStatuses = currentStatuses;

            return changedProcesses;
        }

        /// <summary>
        /// Get current running processes
        /// </summary>
        private List<object> GetCurrentProcesses()
        {
            try
            {
                var processes = processService.GetRunningProcesses();
                var suspiciousProcesses = processService.GetSuspiciousProcesses();
                var suspiciousNames = new HashSet<string>(suspiciousProcesses.Select(p => p.Name));

                return processes.Select(p => new
                {
                    pid = p.Pid,
                    parentPid = p.ParentPid, // Thêm parent process ID
                    name = p.Name,
                    description = p.Description ?? "",
                    memoryUsage = p.MemoryUsage,
                    threadCount = p.ThreadCount,
                    isSuspicious = suspiciousNames.Contains(p.Name),
                    windowTitle = p.WindowTitle ?? "",
                    filePath = p.FilePath ?? "",
                    status = p.Status.ToString(),
                    startTime = p.StartTime // Thêm start time
                } as object).ToList();
            }
            catch
            {
                return new List<object>();
            }
        }

        private Microsoft.Extensions.Logging.ILogger<MonitoringForm>? _logger =>
            LoggerProvider.CreateLogger<MonitoringForm>();

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Cleanup SignalR events before closing
            CleanupSignalREvents();
            
            // Stop timers
            if (dataUploadTimer != null)
            {
                dataUploadTimer.Stop();
                dataUploadTimer.Dispose();
            }
            
            if (monitoringTimer != null)
            {
                monitoringTimer.Stop();
                monitoringTimer.Dispose();
            }
            base.OnFormClosing(e);
        }
    }
}
