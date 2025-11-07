using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using AntdUI;
using ComputerMonitoringClient.Services;
using ComputerMonitoringClient.Models;
using ComputerMonitoringClient.Utils;

namespace ComputerMonitoringClient.Views
{
    public partial class ReportForm : AntdUI.Window
    {
        private readonly MonitoringService monitoringService;
        private readonly AuthenticationService authService;

        private AntdUI.Panel headerPanel;
        private AntdUI.Label lblTitle;
        private AntdUI.Label lblSessionInfo;
        private AntdUI.Panel contentPanel;
        private AntdUI.Label lblTotalLogs;
        private AntdUI.Label lblInfoLogs;
        private AntdUI.Label lblWarningLogs;
        private AntdUI.Label lblErrorLogs;
        private AntdUI.Label lblSuccessLogs;
        private DataGridView dgvLogs;
        private AntdUI.Button btnExport;
        private AntdUI.Button btnRefresh;
        private AntdUI.Button btnClose;

        public ReportForm()
        {
            monitoringService = MonitoringService.Instance;
            authService = AuthenticationService.Instance;
            InitializeComponent();
            LoadReport();
        }

        private void InitializeComponent()
        {
            this.Text = "Báo cáo giám sát";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Constants.Colors.Background;

            // Header Panel
            headerPanel = new AntdUI.Panel
            {
                Location = new Point(20, 20),
                Size = new Size(960, 80),
                Back = Constants.Colors.Primary,
                Shadow = Constants.UI.DefaultShadow,
                Radius = Constants.UI.DefaultRadius
            };
            this.Controls.Add(headerPanel);

            // Title
            lblTitle = new AntdUI.Label
            {
                Text = "BÁO CÁO GIÁM SÁT",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Constants.Colors.White,
                Location = new Point(20, 15),
                Size = new Size(400, 30)
            };
            headerPanel.Controls.Add(lblTitle);

            // Session Info
            var session = authService.CurrentSession;
            lblSessionInfo = new AntdUI.Label
            {
                Text = session != null ? $"Phiên: {session}" : "Chưa đăng nhập",
                Font = new Font("Segoe UI", 10),
                ForeColor = Constants.Colors.White,
                Location = new Point(20, 50),
                Size = new Size(600, 25)
            };
            headerPanel.Controls.Add(lblSessionInfo);

            // Content Panel
            contentPanel = new AntdUI.Panel
            {
                Location = new Point(20, 120),
                Size = new Size(960, 540),
                Back = Constants.Colors.White,
                Shadow = Constants.UI.DefaultShadow,
                Radius = Constants.UI.DefaultRadius
            };
            this.Controls.Add(contentPanel);

            // Statistics Labels
            int statsY = 20;
            lblTotalLogs = CreateStatLabel("Tổng số log: 0", new Point(20, statsY), Constants.Colors.Info);
            contentPanel.Controls.Add(lblTotalLogs);

            lblInfoLogs = CreateStatLabel("Info: 0", new Point(200, statsY), Constants.Colors.Info);
            contentPanel.Controls.Add(lblInfoLogs);

            lblSuccessLogs = CreateStatLabel("Success: 0", new Point(350, statsY), Constants.Colors.Success);
            contentPanel.Controls.Add(lblSuccessLogs);

            lblWarningLogs = CreateStatLabel("Warning: 0", new Point(500, statsY), Constants.Colors.Warning);
            contentPanel.Controls.Add(lblWarningLogs);

            lblErrorLogs = CreateStatLabel("Error: 0", new Point(650, statsY), Constants.Colors.Error);
            contentPanel.Controls.Add(lblErrorLogs);

            // DataGridView
            dgvLogs = new DataGridView
            {
                Location = new Point(20, 70),
                Size = new Size(920, 390),
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false
            };
            
            dgvLogs.Columns.Add("Time", "Thời gian");
            dgvLogs.Columns.Add("Type", "Loại");
            dgvLogs.Columns.Add("Message", "Thông điệp");
            dgvLogs.Columns["Time"].Width = 180;
            dgvLogs.Columns["Type"].Width = 100;
            
            contentPanel.Controls.Add(dgvLogs);

            // Refresh Button
            btnRefresh = new AntdUI.Button
            {
                Text = "Làm mới",
                Location = new Point(20, 480),
                Size = new Size(150, 45),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Type = AntdUI.TTypeMini.Primary,
                BorderWidth = 0f,
                Radius = Constants.UI.DefaultRadius
            };
            btnRefresh.Click += BtnRefresh_Click;
            contentPanel.Controls.Add(btnRefresh);

            // Export Button
            btnExport = new AntdUI.Button
            {
                Text = "Xuất báo cáo",
                Location = new Point(190, 480),
                Size = new Size(150, 45),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Type = AntdUI.TTypeMini.Success,
                BorderWidth = 0f,
                Radius = Constants.UI.DefaultRadius
            };
            btnExport.Click += BtnExport_Click;
            contentPanel.Controls.Add(btnExport);

            // Close Button
            btnClose = new AntdUI.Button
            {
                Text = "Đóng",
                Location = new Point(790, 480),
                Size = new Size(150, 45),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Type = AntdUI.TTypeMini.Default,
                BorderWidth = 1f,
                Radius = Constants.UI.DefaultRadius
            };
            btnClose.Click += BtnClose_Click;
            contentPanel.Controls.Add(btnClose);
        }

        private AntdUI.Label CreateStatLabel(string text, Point location, Color color)
        {
            return new AntdUI.Label
            {
                Text = text,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = color,
                Location = location,
                Size = new Size(140, 30)
            };
        }

        private void LoadReport()
        {
            var logs = monitoringService.Logs;
            
            // Update statistics
            lblTotalLogs.Text = $"Tổng số log: {logs.Count}";
            lblInfoLogs.Text = $"Info: {logs.Count(l => l.Type == LogType.Info)}";
            lblSuccessLogs.Text = $"Success: {logs.Count(l => l.Type == LogType.Success)}";
            lblWarningLogs.Text = $"Warning: {logs.Count(l => l.Type == LogType.Warning)}";
            lblErrorLogs.Text = $"Error: {logs.Count(l => l.Type == LogType.Error)}";

            // Load logs into grid
            dgvLogs.Rows.Clear();
            foreach (var log in logs.OrderByDescending(l => l.Timestamp))
            {
                var row = new DataGridViewRow();
                row.CreateCells(dgvLogs);
                row.Cells[0].Value = log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
                row.Cells[1].Value = log.Type.ToString();
                row.Cells[2].Value = log.Message;
                
                // Set row color based on log type
                switch (log.Type)
                {
                    case LogType.Warning:
                        row.DefaultCellStyle.ForeColor = Constants.Colors.Warning;
                        break;
                    case LogType.Error:
                        row.DefaultCellStyle.ForeColor = Constants.Colors.Error;
                        break;
                    case LogType.Success:
                        row.DefaultCellStyle.ForeColor = Constants.Colors.Success;
                        break;
                }
                
                dgvLogs.Rows.Add(row);
            }
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            LoadReport();
            AntdUI.Notification.success(this, "Làm mới",
                "Đã cập nhật báo cáo mới nhất",
                AntdUI.TAlignFrom.BR, Font);
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            // TODO: Implement export to CSV/Excel
            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Filter = "CSV File|*.csv|Text File|*.txt",
                Title = "Xuất báo cáo",
                FileName = $"BaoCaoGiamSat_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var logs = monitoringService.Logs;
                    System.IO.File.WriteAllLines(saveDialog.FileName,
                        logs.Select(l => $"{l.Timestamp:yyyy-MM-dd HH:mm:ss},{l.Type},{l.Message}"));
                    
                    AntdUI.Notification.success(this, "Xuất báo cáo",
                        $"Đã xuất báo cáo thành công!\n{saveDialog.FileName}",
                        AntdUI.TAlignFrom.BR, Font);
                }
                catch (Exception ex)
                {
                    AntdUI.Modal.open(new AntdUI.Modal.Config(this, "Lỗi", $"Không thể xuất báo cáo: {ex.Message}")
                    {
                        Icon = AntdUI.TType.Error
                    });
                }
            }
        }

        private void BtnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
