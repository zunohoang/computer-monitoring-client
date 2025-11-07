using System;
using System.Drawing;
using System.Windows.Forms;
using AntdUI;
using ComputerMonitoringClient.Utils;

namespace ComputerMonitoringClient.Views
{
    public partial class SettingsForm : AntdUI.Window
    {
        private AntdUI.Panel mainPanel;
        private AntdUI.Label lblTitle;
        private AntdUI.Label lblMonitoringInterval;
        private AntdUI.InputNumber numMonitoringInterval;
        private AntdUI.Label lblAutoStart;
        private AntdUI.Switch swAutoStart;
        private AntdUI.Label lblNotifications;
        private AntdUI.Switch swNotifications;
        private AntdUI.Label lblSaveLog;
        private AntdUI.Switch swSaveLog;
        private AntdUI.Button btnSave;
        private AntdUI.Button btnCancel;

        public SettingsForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Cài đặt hệ thống";
            this.Size = new Size(600, 500);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.BackColor = Constants.Colors.Background;

            // Main Panel
            mainPanel = new AntdUI.Panel
            {
                Location = new Point(20, 20),
                Size = new Size(560, 460),
                Back = Constants.Colors.White,
                Shadow = Constants.UI.DefaultShadow,
                Radius = Constants.UI.DefaultRadius
            };
            this.Controls.Add(mainPanel);

            // Title
            lblTitle = new AntdUI.Label
            {
                Text = "CÀI ĐẶT HỆ THỐNG",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Constants.Colors.Primary,
                Location = new Point(30, 30),
                Size = new Size(500, 35)
            };
            mainPanel.Controls.Add(lblTitle);

            // Monitoring Interval Label
            lblMonitoringInterval = new AntdUI.Label
            {
                Text = "Khoảng thời gian giám sát (giây):",
                Font = new Font("Segoe UI", 11),
                ForeColor = Constants.Colors.TextPrimary,
                Location = new Point(30, 100),
                Size = new Size(250, 30)
            };
            mainPanel.Controls.Add(lblMonitoringInterval);

            // Monitoring Interval Input
            numMonitoringInterval = new AntdUI.InputNumber
            {
                Location = new Point(300, 100),
                Size = new Size(200, 40),
                Font = new Font("Segoe UI", 11),
                Value = 1,
                Minimum = 1,
                Maximum = 60
            };
            mainPanel.Controls.Add(numMonitoringInterval);

            // Auto Start Label
            lblAutoStart = new AntdUI.Label
            {
                Text = "Tự động bắt đầu giám sát:",
                Font = new Font("Segoe UI", 11),
                ForeColor = Constants.Colors.TextPrimary,
                Location = new Point(30, 170),
                Size = new Size(250, 30)
            };
            mainPanel.Controls.Add(lblAutoStart);

            // Auto Start Switch
            swAutoStart = new AntdUI.Switch
            {
                Location = new Point(300, 175),
                Size = new Size(60, 30),
                Checked = false
            };
            mainPanel.Controls.Add(swAutoStart);

            // Notifications Label
            lblNotifications = new AntdUI.Label
            {
                Text = "Hiển thị thông báo:",
                Font = new Font("Segoe UI", 11),
                ForeColor = Constants.Colors.TextPrimary,
                Location = new Point(30, 230),
                Size = new Size(250, 30)
            };
            mainPanel.Controls.Add(lblNotifications);

            // Notifications Switch
            swNotifications = new AntdUI.Switch
            {
                Location = new Point(300, 235),
                Size = new Size(60, 30),
                Checked = true
            };
            mainPanel.Controls.Add(swNotifications);

            // Save Log Label
            lblSaveLog = new AntdUI.Label
            {
                Text = "Tự động lưu log:",
                Font = new Font("Segoe UI", 11),
                ForeColor = Constants.Colors.TextPrimary,
                Location = new Point(30, 290),
                Size = new Size(250, 30)
            };
            mainPanel.Controls.Add(lblSaveLog);

            // Save Log Switch
            swSaveLog = new AntdUI.Switch
            {
                Location = new Point(300, 295),
                Size = new Size(60, 30),
                Checked = true
            };
            mainPanel.Controls.Add(swSaveLog);

            // Save Button
            btnSave = new AntdUI.Button
            {
                Text = "Lưu cài đặt",
                Location = new Point(30, 380),
                Size = new Size(240, 50),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Type = AntdUI.TTypeMini.Primary,
                BorderWidth = 0f,
                Radius = Constants.UI.DefaultRadius
            };
            btnSave.Click += BtnSave_Click;
            mainPanel.Controls.Add(btnSave);

            // Cancel Button
            btnCancel = new AntdUI.Button
            {
                Text = "Hủy",
                Location = new Point(290, 380),
                Size = new Size(240, 50),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Type = AntdUI.TTypeMini.Default,
                BorderWidth = 1f,
                Radius = Constants.UI.DefaultRadius
            };
            btnCancel.Click += BtnCancel_Click;
            mainPanel.Controls.Add(btnCancel);
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            // TODO: Lưu cài đặt vào file hoặc registry
            AntdUI.Notification.success(this, "Thành công",
                "Đã lưu cài đặt thành công!",
                AntdUI.TAlignFrom.BR, Font);
            
            this.Close();
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
