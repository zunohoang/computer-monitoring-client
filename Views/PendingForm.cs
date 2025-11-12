using System;
using System.Drawing;
using System.Windows.Forms;
using AntdUI;
using ComputerMonitoringClient.Utils;
using ComputerMonitoringClient.Services;
using ComputerMonitoringClient.Dtos;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ComputerMonitoringClient.Views
{
    public partial class PendingForm : AntdUI.Window
    {
        private AntdUI.Panel mainPanel = null!;
        private AntdUI.Label lblTitle = null!;
        private AntdUI.Label lblMessage = null!;
        private AntdUI.Label lblStatus = null!;
        private AntdUI.Spin spinLoader = null!;
        private AntdUI.Button btnCancel = null!;

        private readonly JoinRoomResponse joinResponse;
        private readonly string accessCode;

        public PendingForm(JoinRoomResponse response, string accessCode)
        {
            this.joinResponse = response;
            this.accessCode = accessCode;
            
            InitializeComponent();
            ConnectToSignalR();
        }

        private void InitializeComponent()
        {
            this.Text = "Chờ phê duyệt";
            this.Size = new Size(500, 400);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.BackColor = Constants.Colors.Background;

            // Main Panel
            mainPanel = new AntdUI.Panel
            {
                Location = new Point(10, 10),
                Size = new Size(470, 370),
                Back = Constants.Colors.White,
                Shadow = Constants.UI.DefaultShadow,
                Radius = Constants.UI.DefaultRadius
            };
            this.Controls.Add(mainPanel);

            // Title
            lblTitle = new AntdUI.Label
            {
                Text = "CHỜ PHÊ DUYỆT",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Constants.Colors.Warning,
                Location = new Point(120, 40),
                Size = new Size(250, 40)
            };
            mainPanel.Controls.Add(lblTitle);

            // Spinner
            spinLoader = new AntdUI.Spin
            {
                Location = new Point(200, 100),
                Size = new Size(70, 70)
            };
            mainPanel.Controls.Add(spinLoader);

            // Message
            lblMessage = new AntdUI.Label
            {
                Text = "Yêu cầu tham gia phòng thi của bạn đang được xem xét.\nVui lòng chờ giám thị phê duyệt.",
                Font = new Font("Segoe UI", 11),
                ForeColor = Constants.Colors.TextPrimary,
                Location = new Point(30, 190),
                Size = new Size(410, 60),
                TextAlign = ContentAlignment.MiddleCenter
            };
            mainPanel.Controls.Add(lblMessage);

            // Status info
            lblStatus = new AntdUI.Label
            {
                Text = $"Họ tên: {joinResponse.fullName}\nMã dự thi: {joinResponse.sbd}\nPhòng thi: {accessCode}",
                Font = new Font("Segoe UI", 10),
                ForeColor = Constants.Colors.TextSecondary,
                Location = new Point(30, 260),
                Size = new Size(410, 60),
                TextAlign = ContentAlignment.MiddleCenter
            };
            mainPanel.Controls.Add(lblStatus);

            // Cancel Button
            btnCancel = new AntdUI.Button
            {
                Text = "Hủy",
                Location = new Point(185, 320),
                Size = new Size(100, 35),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Type = AntdUI.TTypeMini.Default,
                BorderWidth = 1f,
                Radius = Constants.UI.DefaultRadius
            };
            btnCancel.Click += BtnCancel_Click;
            mainPanel.Controls.Add(btnCancel);
        }

        /// <summary>
        /// Connect to SignalR hub and listen for status updates
        /// </summary>
        private async void ConnectToSignalR()
        {
            try
            {
                var hubClient = MonitoringHubClient.Instance;

                // Register event handlers
                // Use the new detailed event for more information
                hubClient.OnAttemptStatusUpdated += HandleAttemptStatusUpdate;
                
                // Also keep the simple event for backward compatibility
                hubClient.OnStatusUpdated += HandleStatusUpdate;
                
                hubClient.OnConnected += () =>
                {
                    SafeInvoke(() =>
                    {
                        _logger?.LogInformation("SignalR connected, waiting for status updates...");
                    });
                };
                hubClient.OnError += (error) =>
                {
                    SafeInvoke(() =>
                    {
                        _logger?.LogError("SignalR error: {Error}", error);
                        lblMessage.Text = $"Lỗi kết nối: {error}\nĐang thử kết nối lại...";
                    });
                };
                hubClient.OnDisconnected += (ex) =>
                {
                    SafeInvoke(() =>
                    {
                        _logger?.LogWarning("SignalR disconnected: {Error}", ex?.Message);
                    });
                };

                // Connect to hub with token and attemptId
                await hubClient.ConnectAsync(joinResponse.token, joinResponse.attemptId);
            }
            catch (Exception ex)
            {
                AntdUI.Modal.open(new AntdUI.Modal.Config(this, "Lỗi kết nối",
                    $"Không thể kết nối tới server để theo dõi trạng thái!\n{ex.Message}")
                {
                    Icon = AntdUI.TType.Error,
                    OkText = "Đóng",
                    OnOk = (config) =>
                    {
                        this.Close();
                        return true;
                    }
                });
            }
        }

        /// <summary>
        /// Handle detailed attempt status updates from SignalR
        /// </summary>
        private void HandleAttemptStatusUpdate(AttemptStatusUpdateDto update)
        {
            SafeInvoke(() =>
            {
                _logger?.LogInformation(
                    "Attempt status updated - ID: {AttemptId}, Status: {Status}, Time: {Timestamp}", 
                    update.attemptId, 
                    update.status, 
                    update.timestamp);

                // Update UI with detailed information if needed
                if (update.attempt != null)
                {
                    lblStatus.Text = $"Họ tên: {update.attempt.fullName}\n" +
                                   $"SBD: {update.attempt.sbd}\n" +
                                   $"Phòng thi: {accessCode}\n" +
                                   $"Cập nhật: {update.timestamp.ToLocalTime():HH:mm:ss}";
                }

                // Handle status changes
                ProcessStatusChange(update.status, update.attempt?.fullName ?? joinResponse.fullName);
            });
        }

        /// <summary>
        /// Handle status updates from SignalR (backward compatible)
        /// </summary>
        private void HandleStatusUpdate(string status)
        {
            // This is called by the backward compatible event
            // Most processing is done in HandleAttemptStatusUpdate
            _logger?.LogInformation("Simple status update received: {Status}", status);
        }

        /// <summary>
        /// Process status change and navigate accordingly
        /// </summary>
        private void ProcessStatusChange(string status, string fullName)
        {
            if (status.Equals("approved", StringComparison.OrdinalIgnoreCase))
            {
                // Approved - login and go to monitoring form
                try
                {
                    // Login with stored exam code and room code
                    var authService = AuthenticationService.Instance;
                    string errorMessage;
                    bool loginSuccess = authService.Login(joinResponse.sbd.ToString(), accessCode, out errorMessage);

                    if (loginSuccess)
                    {
                        AntdUI.Notification.success(this, "Đã phê duyệt",
                            $"Bạn đã được chấp nhận vào phòng thi!\nHọ tên: {fullName}",
                            AntdUI.TAlignFrom.BR, Font);

                        this.Hide();
                        MonitoringForm monitoringForm = new MonitoringForm();
                        monitoringForm.FormClosed += (s, args) =>
                        {
                            // Don't disconnect here - keep connection alive
                            // Only clear this form's event handlers
                            this.Close();
                        };
                        monitoringForm.Show();
                    }
                    else
                    {
                        AntdUI.Modal.open(new AntdUI.Modal.Config(this, "Lỗi",
                            $"Không thể đăng nhập: {errorMessage}")
                        {
                            Icon = AntdUI.TType.Error,
                            OkText = "Đóng"
                        });
                    }
                }
                catch (Exception ex)
                {
                    AntdUI.Modal.open(new AntdUI.Modal.Config(this, "Lỗi",
                        $"Lỗi khi mở form giám sát: {ex.Message}")
                    {
                        Icon = AntdUI.TType.Error,
                        OkText = "Đóng"
                    });
                    _logger?.LogError(ex, "Error opening MonitoringForm after approval");
                }
            }
            else if (status.Equals("rejected", StringComparison.OrdinalIgnoreCase))
            {
                // Rejected - show error and close
                AntdUI.Modal.open(new AntdUI.Modal.Config(this, "Bị từ chối",
                    $"Yêu cầu của bạn đã bị từ chối!\n{joinResponse.message}")
                {
                    Icon = AntdUI.TType.Error,
                    OkText = "Đóng",
                    OnOk = (config) =>
                    {
                        // Disconnect when rejected
                        _ = MonitoringHubClient.Instance.DisconnectAsync();
                        this.Close();
                        return true;
                    }
                });
            }
        }

        private ILogger<PendingForm>? _logger => 
            LoggerProvider.CreateLogger<PendingForm>();

        private void BtnCancel_Click(object? sender, EventArgs e)
        {
            var result = AntdUI.Modal.open(new AntdUI.Modal.Config(this, "Xác nhận",
                "Bạn có chắc chắn muốn hủy và quay lại màn hình đăng nhập?")
            {
                Icon = AntdUI.TType.Warn,
                OkText = "Có",
                CancelText = "Không",
                OnOk = (config) =>
                {
                    // Disconnect when user cancels
                    _ = MonitoringHubClient.Instance.DisconnectAsync();
                    this.Close();
                    return true;
                }
            });
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Only clear event handlers, don't disconnect
            // Connection will be reused if user logs in again
            var hubClient = MonitoringHubClient.Instance;
            hubClient.OnAttemptStatusUpdated -= HandleAttemptStatusUpdate;
            hubClient.OnStatusUpdated -= HandleStatusUpdate;
            
            base.OnFormClosing(e);
        }

        /// <summary>
        /// Safe invoke helper that checks if handle is created and uses BeginInvoke
        /// </summary>
        private void SafeInvoke(Action action)
        {
            if (this.IsHandleCreated)
            {
                if (this.InvokeRequired)
                {
                    this.BeginInvoke(action);
                }
                else
                {
                    action();
                }
            }
            else
            {
                // Handle not created yet, just log
                _logger?.LogWarning("Form handle not created yet, skipping UI update");
            }
        }
    }
}
