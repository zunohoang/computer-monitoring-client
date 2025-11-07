using System;
using System.Drawing;
using System.Windows.Forms;
using AntdUI;
using ComputerMonitoringClient.Utils;

namespace ComputerMonitoringClient.Views
{
    public partial class AboutForm : AntdUI.Window
    {
        private AntdUI.Panel mainPanel;
        private AntdUI.Label lblAppName;
        private AntdUI.Label lblVersion;
        private AntdUI.Label lblDescription;
        private AntdUI.Label lblCopyright;
        private AntdUI.Label lblDeveloper;
        private AntdUI.Label lblTechnology;
        private AntdUI.Divider divider1;
        private AntdUI.Divider divider2;
        private AntdUI.Button btnOk;

        public AboutForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Về ứng dụng";
            this.Size = new Size(500, 550);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.BackColor = Constants.Colors.Background;

            // Main Panel
            mainPanel = new AntdUI.Panel
            {
                Location = new Point(20, 20),
                Size = new Size(460, 510),
                Back = Constants.Colors.White,
                Shadow = Constants.UI.DefaultShadow,
                Radius = Constants.UI.DefaultRadius
            };
            this.Controls.Add(mainPanel);

            // App Name
            lblAppName = new AntdUI.Label
            {
                Text = "HỆ THỐNG GIÁM SÁT THI",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = Constants.Colors.Primary,
                Location = new Point(30, 40),
                Size = new Size(400, 40)
            };
            mainPanel.Controls.Add(lblAppName);

            // Version
            lblVersion = new AntdUI.Label
            {
                Text = "Phiên bản 1.0.0",
                Font = new Font("Segoe UI", 12),
                ForeColor = Constants.Colors.TextSecondary,
                Location = new Point(30, 90),
                Size = new Size(400, 30)
            };
            mainPanel.Controls.Add(lblVersion);

            // Divider 1
            divider1 = new AntdUI.Divider
            {
                Location = new Point(30, 130),
                Size = new Size(400, 1)
            };
            mainPanel.Controls.Add(divider1);

            // Description
            lblDescription = new AntdUI.Label
            {
                Text = "Ứng dụng giám sát máy tính trong phòng thi,\n" +
                       "giúp theo dõi và ghi nhận các hoạt động\n" +
                       "của thí sinh trong quá trình làm bài thi.",
                Font = new Font("Segoe UI", 11),
                ForeColor = Constants.Colors.TextPrimary,
                Location = new Point(30, 150),
                Size = new Size(400, 80)
            };
            mainPanel.Controls.Add(lblDescription);

            // Developer
            lblDeveloper = new AntdUI.Label
            {
                Text = "Phát triển bởi: Team Computer Monitoring",
                Font = new Font("Segoe UI", 10),
                ForeColor = Constants.Colors.TextPrimary,
                Location = new Point(30, 250),
                Size = new Size(400, 25)
            };
            mainPanel.Controls.Add(lblDeveloper);

            // Copyright
            lblCopyright = new AntdUI.Label
            {
                Text = $"© {DateTime.Now.Year} Computer Monitoring System.\nAll rights reserved.",
                Font = new Font("Segoe UI", 10),
                ForeColor = Constants.Colors.TextSecondary,
                Location = new Point(30, 285),
                Size = new Size(400, 45)
            };
            mainPanel.Controls.Add(lblCopyright);

            // Divider 2
            divider2 = new AntdUI.Divider
            {
                Location = new Point(30, 340),
                Size = new Size(400, 1)
            };
            mainPanel.Controls.Add(divider2);

            // Technology
            lblTechnology = new AntdUI.Label
            {
                Text = "Công nghệ sử dụng:\n" +
                       "• .NET Framework 4.7.2\n" +
                       "• C# 7.3\n" +
                       "• AntdUI - Modern UI Framework\n" +
                       "• Layered Architecture Pattern",
                Font = new Font("Segoe UI", 10),
                ForeColor = Constants.Colors.TextPrimary,
                Location = new Point(30, 360),
                Size = new Size(400, 100)
            };
            mainPanel.Controls.Add(lblTechnology);

            // OK Button
            btnOk = new AntdUI.Button
            {
                Text = "Đóng",
                Location = new Point(160, 450),
                Size = new Size(140, 45),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Type = AntdUI.TTypeMini.Primary,
                BorderWidth = 0f,
                Radius = Constants.UI.DefaultRadius
            };
            btnOk.Click += BtnOk_Click;
            mainPanel.Controls.Add(btnOk);
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
