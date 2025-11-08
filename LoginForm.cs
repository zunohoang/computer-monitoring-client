using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;
using AntdUI;
using ComputerMonitoringClient.Services;
using ComputerMonitoringClient.Utils;
using ComputerMonitoringClient.Dtos;

namespace ComputerMonitoringClient.Views
{
    public partial class LoginForm : AntdUI.Window
    {
        private AntdUI.Input txtExamCode = null!;
        private AntdUI.Input txtRoomCode = null!;
        private AntdUI.Button btnLogin = null!;
        private AntdUI.Label lblTitle = null!;
        private AntdUI.Label lblExamCode = null!;
        private AntdUI.Label lblRoomCode = null!;
        private AntdUI.Panel mainPanel = null!;

        private readonly AuthenticationService authService;
        private readonly ContestService contestService;
        private readonly DeviceService deviceService;

        public LoginForm()
        {
            authService = AuthenticationService.Instance;
            contestService = new ContestService();
            deviceService = DeviceService.Instance;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Đăng nhập hệ thống giám sát";
            this.Size = new Size(Constants.UI.LoginFormWidth, Constants.UI.LoginFormHeight);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.BackColor = Constants.Colors.Background;

            // Main Panel
            mainPanel = new AntdUI.Panel
            {
                Location = new Point(10, 10),
                Size = new Size(490, 390),
                Back = Constants.Colors.White,
                Shadow = Constants.UI.DefaultShadow,
                Radius = Constants.UI.DefaultRadius
            };
            this.Controls.Add(mainPanel);

            // Title
            lblTitle = new AntdUI.Label
            {
                Text = "HỆ THỐNG GIÁM SÁT THI",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = Constants.Colors.Primary,
                Location = new Point(45, 40),
                Size = new Size(400, 40)
            };
            mainPanel.Controls.Add(lblTitle);

            // Label Mã dự thi
            lblExamCode = new AntdUI.Label
            {
                Text = "Mã dự thi",
                Font = new Font("Segoe UI", 11),
                ForeColor = Constants.Colors.TextPrimary,
                Location = new Point(80, 110),
                Size = new Size(100, 30)
            };
            mainPanel.Controls.Add(lblExamCode);

            // Input Mã dự thi
            txtExamCode = new AntdUI.Input
            {
                Location = new Point(80, 140),
                Size = new Size(330, 40),
                Font = new Font("Segoe UI", 11),
                PlaceholderText = "Nhập mã dự thi"
            };
            mainPanel.Controls.Add(txtExamCode);

            // Label Mã phòng thi
            lblRoomCode = new AntdUI.Label
            {
                Text = "Mã phòng thi",
                Font = new Font("Segoe UI", 11),
                ForeColor = Constants.Colors.TextPrimary,
                Location = new Point(80, 200),
                Size = new Size(120, 30)
            };
            mainPanel.Controls.Add(lblRoomCode);

            // Input Mã phòng thi
            txtRoomCode = new AntdUI.Input
            {
                Location = new Point(80, 230),
                Size = new Size(330, 40),
                Font = new Font("Segoe UI", 11),
                PlaceholderText = "Nhập mã phòng thi"
            };
            txtRoomCode.KeyPress += TxtRoomCode_KeyPress;
            mainPanel.Controls.Add(txtRoomCode);

            // Button Login
            btnLogin = new AntdUI.Button
            {
                Text = "Đăng nhập",
                Location = new Point(80, 300),
                Size = new Size(330, 45),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Type = AntdUI.TTypeMini.Primary,
                BorderWidth = 0f,
                Radius = Constants.UI.DefaultRadius
            };
            btnLogin.Click += BtnLogin_Click;
            mainPanel.Controls.Add(btnLogin);
        }

        private void TxtRoomCode_KeyPress(object? sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                BtnLogin_Click(sender, e);
            }
        }

        private async void BtnLogin_Click(object? sender, EventArgs e)
        {
            string examCode = txtExamCode.Text.Trim();
            string roomCode = txtRoomCode.Text.Trim();

            // Validate input
            if (string.IsNullOrWhiteSpace(examCode))
            {
                AntdUI.Modal.open(new AntdUI.Modal.Config(this, "Thông báo", Constants.Messages.ExamCodeRequired)
                {
                    Icon = AntdUI.TType.Warn,
                    OkText = "Đóng"
                });
                txtExamCode.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(roomCode))
            {
                AntdUI.Modal.open(new AntdUI.Modal.Config(this, "Thông báo", Constants.Messages.RoomCodeRequired)
                {
                    Icon = AntdUI.TType.Warn,
                    OkText = "Đóng"
                });
                txtRoomCode.Focus();
                return;
            }

            // Disable button to prevent double-click
            btnLogin.Enabled = false;
            btnLogin.Text = "Đang đăng nhập...";

            try
            {
                // Get device information
                var locationData = deviceService.GetDetailedLocationInfo();
                var ipAddress = deviceService.GetPublicIP();

                // Parse SBD from exam code (assuming it's numeric)
                if (!int.TryParse(examCode, out int sbd))
                {
                    AntdUI.Modal.open(new AntdUI.Modal.Config(this, "Lỗi", "Mã dự thi phải là số!")
                    {
                        Icon = AntdUI.TType.Error,
                        OkText = "Đóng"
                    });
                    btnLogin.Enabled = true;
                    btnLogin.Text = "Đăng nhập";
                    txtExamCode.Focus();
                    return;
                }

                // Create join room request
                var joinRequest = new JoinRoomRequest
                {
                    accessCode = roomCode,
                    sbd = sbd,
                    ipAddress = ipAddress,
                    location = locationData.GetFullLocationString()
                };

                // Call API to join contest room
                var response = await contestService.JoinContestRoomAsync(joinRequest);

                if (response == null)
                {
                    AntdUI.Modal.open(new AntdUI.Modal.Config(this, "Lỗi", "Không nhận được phản hồi từ server!")
                    {
                        Icon = AntdUI.TType.Error,
                        OkText = "Đóng"
                    });
                    btnLogin.Enabled = true;
                    btnLogin.Text = "Đăng nhập";
                    return;
                }

                // Check response status - handle 3 states: pending, rejected, approved
                if (response.IsPending)
                {
                    // Pending - show pending form and wait for approval via SignalR
                    AntdUI.Notification.info(this, "Chờ phê duyệt",
                        "Yêu cầu của bạn đang chờ giám thị phê duyệt",
                        AntdUI.TAlignFrom.BR, Font);

                    this.Hide();
                    PendingForm pendingForm = new PendingForm(response, roomCode);
                    pendingForm.FormClosed += (s, args) => 
                    {
                        this.Show();
                        btnLogin.Enabled = true;
                        btnLogin.Text = "Đăng nhập";
                    };
                    pendingForm.Show();
                    return;
                }
                else if (response.IsRejected)
                {
                    // Rejected - show error message
                    AntdUI.Modal.open(new AntdUI.Modal.Config(this, "Bị từ chối",
                        $"Yêu cầu tham gia phòng thi của bạn đã bị từ chối!\n" +
                        $"{(string.IsNullOrEmpty(response.message) ? "Vui lòng liên hệ giám thị để biết thêm chi tiết." : response.message)}")
                    {
                        Icon = AntdUI.TType.Error,
                        OkText = "Đóng"
                    });
                    btnLogin.Enabled = true;
                    btnLogin.Text = "Đăng nhập";
                    return;
                }
                else if (response.IsApproved)
                {
                    // Approved - proceed to monitoring form
                    string errorMessage;
                    bool loginSuccess = authService.Login(examCode, roomCode, out errorMessage);

                    if (loginSuccess)
                    {
                        // Connect to SignalR first before navigating to monitoring form
                        try
                        {
                            var hubClient = MonitoringHubClient.Instance;
                            await hubClient.ConnectAsync(response.token, response.attemptId);
                            
                            // Show success notification
                            AntdUI.Notification.success(this, "Thành công",
                                $"{Constants.Messages.LoginSuccess}\n" +
                                $"Họ tên: {response.fullName}\n" +
                                $"SBD: {response.sbd}\n" +
                                $"Phòng thi: {roomCode}",
                                AntdUI.TAlignFrom.BR, Font);

                            // Navigate to monitoring form
                            this.Hide();
                            MonitoringForm monitoringForm = new MonitoringForm();
                            monitoringForm.FormClosed += (s, args) => this.Close();
                            monitoringForm.Show();
                        }
                        catch (Exception signalREx)
                        {
                            AntdUI.Modal.open(new AntdUI.Modal.Config(this, "Cảnh báo kết nối",
                                $"Đăng nhập thành công nhưng không thể kết nối SignalR!\n{signalREx.Message}\n\nBạn vẫn có thể tiếp tục nhưng sẽ không nhận được cập nhật real-time.")
                            {
                                Icon = AntdUI.TType.Warn,
                                OkText = "Tiếp tục",
                                OnOk = (config) =>
                                {
                                    // Navigate anyway even if SignalR fails
                                    this.Hide();
                                    MonitoringForm monitoringForm = new MonitoringForm();
                                    monitoringForm.FormClosed += (s, args) => this.Close();
                                    monitoringForm.Show();
                                    return true;
                                }
                            });
                            btnLogin.Enabled = true;
                            btnLogin.Text = "Đăng nhập";
                        }
                    }
                    else
                    {
                        AntdUI.Modal.open(new AntdUI.Modal.Config(this, "Lỗi", errorMessage)
                        {
                            Icon = AntdUI.TType.Error,
                            OkText = "Đóng"
                        });
                        btnLogin.Enabled = true;
                        btnLogin.Text = "Đăng nhập";
                    }
                }
                else
                {
                    // Unknown status - show error
                    AntdUI.Modal.open(new AntdUI.Modal.Config(this, "Lỗi",
                        $"Trạng thái không xác định: {response.status}\n" +
                        $"{(string.IsNullOrEmpty(response.message) ? "" : response.message)}")
                    {
                        Icon = AntdUI.TType.Error,
                        OkText = "Đóng"
                    });
                    btnLogin.Enabled = true;
                    btnLogin.Text = "Đăng nhập";
                }
            }
            catch (Exception ex)
            {
                AntdUI.Modal.open(new AntdUI.Modal.Config(this, "Lỗi", 
                    $"Không thể kết nối đến server!\n{ex.Message}")
                {
                    Icon = AntdUI.TType.Error,
                    OkText = "Đóng"
                });
                btnLogin.Enabled = true;
                btnLogin.Text = "Đăng nhập";
            }
        }
    }
}
