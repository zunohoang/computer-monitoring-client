using System;
using System.Windows.Forms;
using System.Threading.Tasks;
using AntdUI;
using ComputerMonitoringClient.Services;
using ComputerMonitoringClient.Utils;
using ComputerMonitoringClient.Dtos;
using ComputerMonitoringClient.Networks;
using ComputerMonitoringClient.Configs;

namespace ComputerMonitoringClient.Views
{
    public partial class LoginForm : AntdUI.Window
    {
        private readonly AuthenticationService authService;
        private readonly ContestService contestService;
        private readonly ConfigService configService;

        public LoginForm()
        {
            authService = AuthenticationService.Instance;
            configService = ConfigService.Instance;
            contestService = new ContestService();
            InitializeComponent();
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

            btnLogin.Enabled = false;
            btnLogin.Text = "Đang đăng nhập...";

            try
            {
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

                var joinRequest = new JoinRoomRequest
                {
                    accessCode = roomCode,
                    sbd = sbd,
                    ipAddress = "Unknown",
                    location = "Unknown"
                };

                var response = await contestService.JoinContestRoomAsync(joinRequest);
                AppHttpSession.CurrentContestId = response?.contestId;
                AppHttpSession.CurrentUserId = response?.sbd;
                AppHttpSession.CurrentRoomId = response?.roomId;
                AppHttpSession.Token = response?.token;
                AppHttpSession.CurrentAttemptId = response?.attemptId;
                AppHttpSession.CurrentToken = response?.token;
                ContestConfig.ProcessBlackList = await configService.GetProcessBlackList();

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

                if (response.IsPending)
                {
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
                    string errorMessage;
                    bool loginSuccess = authService.Login(examCode, roomCode, out errorMessage);

                    if (loginSuccess)
                    {
                        try
                        {
                            var hubClient = MonitoringHubClient.Instance;
                            await hubClient.ConnectAsync(response.token, response.attemptId);
                            
                            AntdUI.Notification.success(this, "Thành công",
                                $"{Constants.Messages.LoginSuccess}\n" +
                                $"Họ tên: {response.fullName}\n" +
                                $"SBD: {response.sbd}\n" +
                                $"Phòng thi: {roomCode}",
                                AntdUI.TAlignFrom.BR, Font);

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
