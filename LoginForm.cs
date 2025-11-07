using System;
using System.Drawing;
using System.Windows.Forms;
using AntdUI;
using ComputerMonitoringClient.Services;
using ComputerMonitoringClient.Utils;

namespace ComputerMonitoringClient.Views
{
    public partial class LoginForm : AntdUI.Window
    {
        private AntdUI.Input txtExamCode;
        private AntdUI.Input txtRoomCode;
        private AntdUI.Button btnLogin;
        private AntdUI.Label lblTitle;
        private AntdUI.Label lblExamCode;
        private AntdUI.Label lblRoomCode;
        private AntdUI.Panel mainPanel;

        private readonly AuthenticationService authService;

        public LoginForm()
        {
            authService = AuthenticationService.Instance;
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

        private void TxtRoomCode_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                BtnLogin_Click(sender, e);
            }
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            string examCode = txtExamCode.Text.Trim();
            string roomCode = txtRoomCode.Text.Trim();

            // Sử dụng AuthenticationService để xác thực
            string errorMessage;
            bool loginSuccess = authService.Login(examCode, roomCode, out errorMessage);

            if (!loginSuccess)
            {
                AntdUI.Modal.open(new AntdUI.Modal.Config(this, "Thông báo", errorMessage)
                {
                    Icon = AntdUI.TType.Warn,
                    OkText = "Đóng"
                });

                if (string.IsNullOrWhiteSpace(examCode))
                    txtExamCode.Focus();
                else
                    txtRoomCode.Focus();
                return;
            }

            // Đăng nhập thành công
            AntdUI.Notification.success(this, "Thành công",
                $"{Constants.Messages.LoginSuccess}\nMã dự thi: {examCode}\nPhòng thi: {roomCode}",
                AntdUI.TAlignFrom.BR, Font);

            this.Hide();
            MonitoringForm monitoringForm = new MonitoringForm();
            monitoringForm.FormClosed += (s, args) => this.Close();
            monitoringForm.Show();
        }
    }
}
