using System;
using System.Linq;
using System.Windows.Forms;
using AntdUI;
using ComputerMonitoringClient.Dtos;
using ComputerMonitoringClient.Services;
using ComputerMonitoringClient.Utils;
using ComputerMonitoringClient.Networks;

namespace ComputerMonitoringClient.Views
{
    public partial class MonitoringForm : AntdUI.Window
    {
        private readonly AuthenticationService authService;
        private readonly MonitoringHubClient hubClient;
        private readonly ProcessService processService;
        private readonly ProcessBlockerService processBlocker;

        private AntdUI.Label lblHeader = null!;
        private AntdUI.Label lblStatus = null!;
        private AntdUI.Panel infoPanel = null!;
        private AntdUI.Label lblContestInfo = null!;
        private AntdUI.Label lblUserInfo = null!;
        private AntdUI.Label lblRoomInfo = null!;
        private AntdUI.Label lblAttemptInfo = null!;
        private AntdUI.Button btnLogout = null!;
        private AntdUI.Panel contentPanel = null!;
        private AntdUI.Input txtProcessLog = null!;

        public MonitoringForm()
        {
            authService = AuthenticationService.Instance;
            hubClient = MonitoringHubClient.Instance;
            processService = new ProcessService();
            processBlocker = new ProcessBlockerService(processService);
            InitializeComponent();
            LoadSessionInfo();
            SetupProcessMonitoring();
            SetupProcessBlocker();
        }

        private void InitializeComponent()
        {
            this.Text = "Hệ thống giám sát thi";
            this.Size = new System.Drawing.Size(900, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Constants.Colors.Background;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            // Content Panel
            contentPanel = new AntdUI.Panel
            {
                Location = new System.Drawing.Point(20, 20),
                Size = new System.Drawing.Size(860, 640),
                Back = Constants.Colors.White,
                Shadow = Constants.UI.DefaultShadow,
                Radius = Constants.UI.DefaultRadius
            };
            this.Controls.Add(contentPanel);

            // Header
            lblHeader = new AntdUI.Label
            {
                Text = "ĐANG TRONG PHÒNG THI",
                Font = new System.Drawing.Font("Segoe UI", 18, System.Drawing.FontStyle.Bold),
                ForeColor = Constants.Colors.Primary,
                Location = new System.Drawing.Point(20, 20),
                Size = new System.Drawing.Size(820, 40),
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter
            };
            contentPanel.Controls.Add(lblHeader);

            // Status
            lblStatus = new AntdUI.Label
            {
                Text = "✓ Hệ thống đang hoạt động",
                Font = new System.Drawing.Font("Segoe UI", 14),
                ForeColor = Constants.Colors.Success,
                Location = new System.Drawing.Point(20, 70),
                Size = new System.Drawing.Size(820, 30),
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter
            };
            contentPanel.Controls.Add(lblStatus);

            // Info Panel - Session Information
            infoPanel = new AntdUI.Panel
            {
                Location = new System.Drawing.Point(20, 110),
                Size = new System.Drawing.Size(820, 120),
                Back = System.Drawing.Color.FromArgb(245, 248, 255),
                Radius = 8,
                BorderWidth = 1f,
            };
            contentPanel.Controls.Add(infoPanel);

            lblContestInfo = new AntdUI.Label
            {
                Text = "🏆 Contest ID: --",
                Font = new System.Drawing.Font("Segoe UI", 11, System.Drawing.FontStyle.Bold),
                Location = new System.Drawing.Point(20, 15),
                Size = new System.Drawing.Size(380, 25),
                ForeColor = Constants.Colors.TextPrimary
            };
            infoPanel.Controls.Add(lblContestInfo);

            lblUserInfo = new AntdUI.Label
            {
                Text = "👤 SBD: --",
                Font = new System.Drawing.Font("Segoe UI", 11),
                Location = new System.Drawing.Point(420, 15),
                Size = new System.Drawing.Size(380, 25),
                ForeColor = Constants.Colors.TextPrimary
            };
            infoPanel.Controls.Add(lblUserInfo);

            lblRoomInfo = new AntdUI.Label
            {
                Text = "🚪 Room ID: --",
                Font = new System.Drawing.Font("Segoe UI", 11),
                Location = new System.Drawing.Point(20, 50),
                Size = new System.Drawing.Size(380, 25),
                ForeColor = Constants.Colors.TextPrimary
            };
            infoPanel.Controls.Add(lblRoomInfo);

            lblAttemptInfo = new AntdUI.Label
            {
                Text = "📝 Attempt ID: --",
                Font = new System.Drawing.Font("Segoe UI", 11),
                Location = new System.Drawing.Point(420, 50),
                Size = new System.Drawing.Size(380, 25),
                ForeColor = Constants.Colors.TextPrimary
            };
            infoPanel.Controls.Add(lblAttemptInfo);

            var lblConnection = new AntdUI.Label
            {
                Text = "🔌 SignalR: Đang kết nối...",
                Font = new System.Drawing.Font("Segoe UI", 10),
                Location = new System.Drawing.Point(20, 85),
                Size = new System.Drawing.Size(780, 25),
                ForeColor = Constants.Colors.TextSecondary
            };
            infoPanel.Controls.Add(lblConnection);

            // Update connection status
            var timer = new System.Windows.Forms.Timer { Interval = 2000 };
            timer.Tick += (s, e) => 
            {
                lblConnection.Text = hubClient.IsConnected 
                    ? "🔌 SignalR: ✅ Đã kết nối" 
                    : "🔌 SignalR: ❌ Mất kết nối";
                lblConnection.ForeColor = hubClient.IsConnected 
                    ? Constants.Colors.Success 
                    : Constants.Colors.Error;
            };
            timer.Start();

            // Process Log
            txtProcessLog = new AntdUI.Input
            {
                Location = new System.Drawing.Point(20, 240),
                Size = new System.Drawing.Size(820, 300),
                Multiline = true,
                ReadOnly = true,
                Font = new System.Drawing.Font("Consolas", 9),
                PlaceholderText = "Nhật ký theo dõi tiến trình..."
            };
            contentPanel.Controls.Add(txtProcessLog);

            // Logout Button
            btnLogout = new AntdUI.Button
            {
                Text = "Đăng xuất",
                Location = new System.Drawing.Point(340, 560),
                Size = new System.Drawing.Size(180, 50),
                Font = new System.Drawing.Font("Segoe UI", 12, System.Drawing.FontStyle.Bold),
                Type = AntdUI.TTypeMini.Error,
                BorderWidth = 0f,
                Radius = Constants.UI.DefaultRadius
            };
            btnLogout.Click += BtnLogout_Click;
            contentPanel.Controls.Add(btnLogout);
        }

        private void LoadSessionInfo()
        {
            try
            {
                lblContestInfo.Text = $"🏆 Contest ID: {AppHttpSession.CurrentContestId ?? 0}";
                lblUserInfo.Text = $"👤 SBD: {AppHttpSession.CurrentUserId ?? 0}";
                lblRoomInfo.Text = $"🚪 Room ID: {AppHttpSession.CurrentRoomId ?? 0}";
                lblAttemptInfo.Text = $"📝 Attempt ID: {AppHttpSession.CurrentAttemptId ?? 0}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading session info: {ex.Message}");
            }
        }

        private void SetupProcessMonitoring()
        {
            // Đăng ký sự kiện khi danh sách tiến trình thay đổi (chỉ những tiến trình thay đổi)
            processService.ProcessesChangedDetailed += async (addedProcesses, removedProcesses) =>
            {
                // Thread-safe update UI
                if (InvokeRequired)
                {
                    Invoke(new Action(() =>
                    {
                        var timestamp = DateTime.Now.ToString("HH:mm:ss");
                        var logMessage = "";
                        
                        // Log tiến trình mới mở
                        if (addedProcesses.Count > 0)
                        {
                            logMessage += $"[{timestamp}] ✅ {addedProcesses.Count} tiến trình mới:\n";
                            foreach (var proc in addedProcesses) // Hiển thị tối đa 10
                            {
                                logMessage += $"  + {proc.Name} (PID: {proc.Pid})\n";
                            }
                        }
                        
                        // Log tiến trình đã đóng
                        if (removedProcesses.Count > 0)
                        {
                            logMessage += $"[{timestamp}] ❌ {removedProcesses.Count} tiến trình đã đóng:\n";
                            foreach (var proc in removedProcesses) // Hiển thị tối đa 10
                            {
                                logMessage += $"  - {proc.Name} (PID: {proc.Pid})\n";
                            }
                        }
                        
                        if (!string.IsNullOrEmpty(logMessage))
                        {
                            txtProcessLog.Text = logMessage + "\n" + txtProcessLog.Text;
                            
                            // Giới hạn độ dài log (giữ 2000 ký tự cuối)
                            if (txtProcessLog.Text.Length > 2000)
                            {
                                txtProcessLog.Text = txtProcessLog.Text.Substring(0, 2000);
                            }
                        }
                    }));
                }

                // Gửi lên hub nếu đã kết nối - CHỈ GỬI TIẾN TRÌNH THAY ĐỔI
                if (hubClient.IsConnected && AppHttpSession.CurrentAttemptId.HasValue)
                {
                    try
                    {
                        // Chỉ gửi danh sách tiến trình đã thay đổi (added + removed)  
                        var changedProcesses = addedProcesses.Concat(removedProcesses).ToList(); // TEST: CHỈ 5 PROCESS
                        
                        if (changedProcesses.Count > 0)
                        {
                            var processObjects = changedProcesses.Select(p => new ProcessChangeDto                                  
                            {
                                Pid = p.Pid ?? 0,
                                Name = p.Name ?? "unknown",
                                ParentPid = p.ParentPid ?? 0,
                                Status = addedProcesses.Contains(p) ? "START" : "END",
                                Timestamp = p.Timestamp?.ToUniversalTime() ?? DateTime.UtcNow // FIX: UTC required
                            }).ToList();

                            await hubClient.SendProcessListAsync(
                                (long)AppHttpSession.CurrentAttemptId.Value,
                                processObjects
                            );
                            
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error sending process list: {ex.Message}");
                    }
                }
            };

            // Bắt đầu giám sát
            processService.StartMonitoring(2000); // Kiểm tra mỗi 2 giây
        }

        private void SetupProcessBlocker()
        {
            // Đăng ký sự kiện khi tiến trình bị chặn
            processBlocker.ProcessBlocked += (processName, pid) =>
            {
                // Thread-safe update UI
                if (InvokeRequired)
                {
                    Invoke(new Action(() =>
                    {
                        var timestamp = DateTime.Now.ToString("HH:mm:ss");
                        var blockMessage = $"[{timestamp}] 🚫 CHẶN: {processName} (PID: {pid})\n";
                        txtProcessLog.Text = blockMessage + txtProcessLog.Text;
                        
                        // Giới hạn độ dài log (giữ 2000 ký tự cuối)
                        if (txtProcessLog.Text.Length > 2000)
                        {
                            txtProcessLog.Text = txtProcessLog.Text.Substring(0, 2000);
                        }
                    }));
                }
            };

            // Bắt đầu chặn tiến trình đen
            processBlocker.StartBlocking();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            
            // Shutdown RemoteControlService
            RemoteControlService.Instance.Shutdown();
            
            // Dừng giám sát khi đóng form
            processService.StopMonitoring();
            processBlocker.StopBlocking();
            RemoteControlService.Instance.Shutdown();
            ProcessKillerService.Instance.Shutdown();
        }

        private void BtnLogout_Click(object? sender, EventArgs e)
        {
            var result = AntdUI.Modal.open(new AntdUI.Modal.Config(this, "Xác nhận",
                "Bạn có chắc chắn muốn đăng xuất?")
            {
                Icon = AntdUI.TType.Warn,
                OkText = "Đăng xuất",
                CancelText = "Hủy",
                OnOk = (config) =>
                {
                    try
                    {
                        var task = hubClient.DisconnectAsync();
                        task.Wait();
                    }
                    catch { }
                    
                    authService.Logout();
                    this.Close();
                    return true;
                }
            });
        }
    }
}
