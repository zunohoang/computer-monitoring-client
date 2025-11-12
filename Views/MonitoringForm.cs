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

        private AntdUI.Label lblHeader = null!;
        private AntdUI.Label lblStatus = null!;
        private AntdUI.Button btnLogout = null!;
        private AntdUI.Panel contentPanel = null!;
        private AntdUI.Input txtProcessLog = null!;

        public MonitoringForm()
        {
            authService = AuthenticationService.Instance;
            hubClient = MonitoringHubClient.Instance;
            processService = new ProcessService();
            InitializeComponent();
            SetupProcessMonitoring();
        }

        private void InitializeComponent()
        {
            this.Text = "Hệ thống giám sát thi";
            this.Size = new System.Drawing.Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Constants.Colors.Background;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            // Content Panel
            contentPanel = new AntdUI.Panel
            {
                Location = new System.Drawing.Point(20, 20),
                Size = new System.Drawing.Size(760, 540),
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
                Size = new System.Drawing.Size(720, 40),
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter
            };
            contentPanel.Controls.Add(lblHeader);

            // Status
            lblStatus = new AntdUI.Label
            {
                Text = "✓ Hệ thống đang hoạt động",
                Font = new System.Drawing.Font("Segoe UI", 14),
                ForeColor = Constants.Colors.Success,
                Location = new System.Drawing.Point(20, 80),
                Size = new System.Drawing.Size(720, 40),
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter
            };
            contentPanel.Controls.Add(lblStatus);

            // Process Log
            txtProcessLog = new AntdUI.Input
            {
                Location = new System.Drawing.Point(20, 140),
                Size = new System.Drawing.Size(720, 300),
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
                Location = new System.Drawing.Point(290, 460),
                Size = new System.Drawing.Size(180, 50),
                Font = new System.Drawing.Font("Segoe UI", 12, System.Drawing.FontStyle.Bold),
                Type = AntdUI.TTypeMini.Error,
                BorderWidth = 0f,
                Radius = Constants.UI.DefaultRadius
            };
            btnLogout.Click += BtnLogout_Click;
            contentPanel.Controls.Add(btnLogout);
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
                        var changedProcesses = addedProcesses.Concat(removedProcesses).Take(5).ToList(); // TEST: CHỈ 5 PROCESS
                        
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

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            // Dừng giám sát khi đóng form
            processService.StopMonitoring();
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
