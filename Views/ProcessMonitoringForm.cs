using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AntdUI;
using ComputerMonitoringClient.Services;
using ComputerMonitoringClient.Utils;
using ComputerMonitoringClient.Dtos;

namespace ComputerMonitoringClient.Views
{
    public partial class ProcessMonitoringForm : AntdUI.Window
    {
        private readonly ProcessService processService;
        private List<Proccess> currentProcesses;
        private System.Windows.Forms.Timer refreshTimer;

        // UI Controls
        private AntdUI.Panel headerPanel;
        private AntdUI.Label lblTitle;
        private AntdUI.Button btnRefresh;
        private AntdUI.Button btnClose;
        private AntdUI.Button btnShowSuspicious;
        private AntdUI.Button btnExportList;

        private AntdUI.Panel contentPanel;
        private DataGridView dgvProcesses;
        private AntdUI.Label lblProcessCount;
        private AntdUI.Label lblSuspiciousCount;
        private AntdUI.Label lblSystemResources;

        public ProcessMonitoringForm()
        {
            processService = ProcessService.Instance;
            currentProcesses = new List<Proccess>();
            InitializeComponent();
            LoadProcessData();
        }

        private void InitializeComponent()
        {
            this.Text = "Giám sát tiến trình hệ thống";
            this.Size = new Size(1200, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Constants.Colors.Background;

            // Header Panel
            headerPanel = new AntdUI.Panel
            {
                Location = new Point(20, 20),
                Size = new Size(1150, 80),
                Back = Constants.Colors.Primary,
                Radius = Constants.UI.DefaultRadius
            };
            this.Controls.Add(headerPanel);

            // Title
            lblTitle = new AntdUI.Label
            {
                Text = "GIÁM SÁT TIẾN TRÌNH HỆ THỐNG",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Constants.Colors.White,
                Location = new Point(20, 25),
                Size = new Size(400, 30)
            };
            headerPanel.Controls.Add(lblTitle);

            // Buttons in header
            btnRefresh = new AntdUI.Button
            {
                Text = "Làm mới",
                Location = new Point(700, 20),
                Size = new Size(100, 40),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Type = AntdUI.TTypeMini.Success,
                BorderWidth = 0f,
                Radius = Constants.UI.DefaultRadius,
                ForeColor = Constants.Colors.White
            };
            btnRefresh.Click += BtnRefresh_Click;
            headerPanel.Controls.Add(btnRefresh);

            btnShowSuspicious = new AntdUI.Button
            {
                Text = "Tiến trình đáng ngờ",
                Location = new Point(810, 20),
                Size = new Size(140, 40),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Type = AntdUI.TTypeMini.Error, // Use Error instead of Warning
                BorderWidth = 0f,
                Radius = Constants.UI.DefaultRadius,
                ForeColor = Constants.Colors.White
            };
            btnShowSuspicious.Click += BtnShowSuspicious_Click;
            headerPanel.Controls.Add(btnShowSuspicious);

            btnExportList = new AntdUI.Button
            {
                Text = "Xuất danh sách",
                Location = new Point(960, 20),
                Size = new Size(120, 40),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Type = AntdUI.TTypeMini.Default,
                BorderWidth = 1f,
                Radius = Constants.UI.DefaultRadius,
                ForeColor = Constants.Colors.White
            };
            btnExportList.Click += BtnExportList_Click;
            headerPanel.Controls.Add(btnExportList);

            btnClose = new AntdUI.Button
            {
                Text = "Đóng",
                Location = new Point(1090, 20),
                Size = new Size(60, 40),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Type = AntdUI.TTypeMini.Error,
                BorderWidth = 0f,
                Radius = Constants.UI.DefaultRadius,
                ForeColor = Constants.Colors.White
            };
            btnClose.Click += BtnClose_Click;
            headerPanel.Controls.Add(btnClose);

            // Content Panel
            contentPanel = new AntdUI.Panel
            {
                Location = new Point(20, 120),
                Size = new Size(1150, 640),
                Back = Constants.Colors.White,
                Shadow = Constants.UI.DefaultShadow,
                Radius = Constants.UI.DefaultRadius
            };
            this.Controls.Add(contentPanel);

            // Statistics Labels
            lblProcessCount = new AntdUI.Label
            {
                Text = "Tổng tiến trình: 0",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Constants.Colors.ReadableInfo,
                Location = new Point(20, 20),
                Size = new Size(200, 30)
            };
            contentPanel.Controls.Add(lblProcessCount);

            lblSuspiciousCount = new AntdUI.Label
            {
                Text = "Tiến trình đáng ngờ: 0",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Constants.Colors.ReadableWarning,
                Location = new Point(230, 20),
                Size = new Size(200, 30)
            };
            contentPanel.Controls.Add(lblSuspiciousCount);

            lblSystemResources = new AntdUI.Label
            {
                Text = "Đang tải thông tin tài nguyên...",
                Font = new Font("Segoe UI", 10),
                ForeColor = Constants.Colors.ReadableText,
                Location = new Point(450, 20),
                Size = new Size(600, 30)
            };
            contentPanel.Controls.Add(lblSystemResources);

            // Process DataGridView
            dgvProcesses = new DataGridView
            {
                Location = new Point(20, 60),
                Size = new Size(1110, 560),
                BackgroundColor = Constants.Colors.LogBackground,
                BorderStyle = BorderStyle.FixedSingle,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                Font = new Font("Segoe UI", 9)
            };

            // Add columns
            dgvProcesses.Columns.Add("PID", "PID");
            dgvProcesses.Columns.Add("Name", "Tên tiến trình");
            dgvProcesses.Columns.Add("Description", "Mô tả");
            dgvProcesses.Columns.Add("Memory", "Bộ nhớ (MB)");
            dgvProcesses.Columns.Add("Threads", "Threads");
            dgvProcesses.Columns.Add("StartTime", "Thời gian bắt đầu");
            dgvProcesses.Columns.Add("Status", "Trạng thái");

            // Set column widths
            dgvProcesses.Columns["PID"].Width = 80;
            dgvProcesses.Columns["Name"].Width = 200;
            dgvProcesses.Columns["Description"].Width = 300;
            dgvProcesses.Columns["Memory"].Width = 100;
            dgvProcesses.Columns["Threads"].Width = 80;
            dgvProcesses.Columns["StartTime"].Width = 150;
            dgvProcesses.Columns["Status"].Width = 100;

            // Event handlers
            dgvProcesses.CellDoubleClick += DgvProcesses_CellDoubleClick;
            dgvProcesses.SelectionChanged += DgvProcesses_SelectionChanged;

            contentPanel.Controls.Add(dgvProcesses);

            // Setup auto-refresh timer
            refreshTimer = new System.Windows.Forms.Timer();
            refreshTimer.Interval = 5000; // 5 seconds
            refreshTimer.Tick += RefreshTimer_Tick;
            refreshTimer.Start();
        }

        private void LoadProcessData()
        {
            try
            {
                currentProcesses = processService.GetRunningProcesses();
                UpdateProcessGrid();
                UpdateStatistics();
                UpdateSystemResources();
            }
            catch (Exception ex)
            {
                AntdUI.Modal.open(new AntdUI.Modal.Config(this, "Lỗi", $"Không thể tải dữ liệu tiến trình: {ex.Message}")
                {
                    Icon = AntdUI.TType.Error,
                    OkText = "Đóng"
                });
            }
        }

        private void UpdateProcessGrid()
        {
            dgvProcesses.Rows.Clear();

            foreach (var process in currentProcesses)
            {
                var row = new DataGridViewRow();
                row.CreateCells(dgvProcesses);

                row.Cells[0].Value = process.Pid;
                row.Cells[1].Value = process.Name;
                row.Cells[2].Value = process.Description;
                row.Cells[3].Value = process.MemoryUsage > 0 ? (process.MemoryUsage / (1024 * 1024)).ToString("F1") : "N/A";
                row.Cells[4].Value = process.ThreadCount > 0 ? process.ThreadCount.ToString() : "N/A";
                row.Cells[5].Value = process.StartTime != DateTime.MinValue ? process.StartTime.ToString("HH:mm:ss") : "N/A";
                row.Cells[6].Value = process.IsSuspicious ? "ĐÁNG NGỜ" : "Bình thường";

                // Color suspicious processes
                if (process.IsSuspicious)
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(255, 235, 235);
                    row.DefaultCellStyle.ForeColor = Constants.Colors.ReadableError;
                }

                dgvProcesses.Rows.Add(row);
            }
        }

        private void UpdateStatistics()
        {
            var suspiciousCount = currentProcesses.Count(p => p.IsSuspicious);
            
            lblProcessCount.Text = $"Tổng tiến trình: {currentProcesses.Count}";
            lblSuspiciousCount.Text = $"Tiến trình đáng ngờ: {suspiciousCount}";

            // Update suspicious count color
            lblSuspiciousCount.ForeColor = suspiciousCount > 0 ? Constants.Colors.ReadableError : Constants.Colors.ReadableSuccess;
        }

        private void UpdateSystemResources()
        {
            try
            {
                var resourceInfo = processService.GetSystemResourceInfo();
                // Extract first line only for compact display
                var lines = resourceInfo.Split('\n');
                var compactInfo = "";
                
                // Find CPU and Memory lines
                foreach (var line in lines)
                {
                    if (line.Contains("CPU Usage:") || line.Contains("Available Memory:") || line.Contains("Total Processes:"))
                    {
                        compactInfo += line.Trim().Replace("•", "") + " | ";
                    }
                }
                
                lblSystemResources.Text = compactInfo.TrimEnd(' ', '|') ?? "Không thể lấy thông tin tài nguyên";
            }
            catch
            {
                lblSystemResources.Text = "Không thể lấy thông tin tài nguyên";
            }
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            // Force refresh cache for immediate update
            processService.RefreshCache();
            LoadProcessData();
            AntdUI.Notification.info(this, "Thông báo", "Đã làm mới danh sách tiến trình", AntdUI.TAlignFrom.TR, Font);
        }

        private void BtnShowSuspicious_Click(object sender, EventArgs e)
        {
            try
            {
                var suspiciousProcesses = processService.GetSuspiciousProcesses();
                
                if (suspiciousProcesses.Count == 0)
                {
                    AntdUI.Modal.open(new AntdUI.Modal.Config(this, "Thông báo", "Không phát hiện tiến trình đáng ngờ nào.")
                    {
                        Icon = AntdUI.TType.Success,
                        OkText = "Đóng"
                    });
                    return;
                }

                var message = $"Phát hiện {suspiciousProcesses.Count} tiến trình đáng ngờ:\n\n";
                foreach (var proc in suspiciousProcesses.Take(10)) // Show top 10
                {
                    message += $"• {proc.Name} (PID: {proc.Pid}) - {proc.Description}\n";
                }

                if (suspiciousProcesses.Count > 10)
                {
                    message += $"... và {suspiciousProcesses.Count - 10} tiến trình khác.";
                }

                AntdUI.Modal.open(new AntdUI.Modal.Config(this, "Tiến trình đáng ngờ", message)
                {
                    Icon = AntdUI.TType.Warn,
                    OkText = "Đóng",
                    Width = 600
                });
            }
            catch (Exception ex)
            {
                AntdUI.Modal.open(new AntdUI.Modal.Config(this, "Lỗi", $"Lỗi khi kiểm tra tiến trình đáng ngờ: {ex.Message}")
                {
                    Icon = AntdUI.TType.Error,
                    OkText = "Đóng"
                });
            }
        }

        private void BtnExportList_Click(object sender, EventArgs e)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                    FileName = $"ProcessList_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
                };

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    var content = new System.Text.StringBuilder();
                    content.AppendLine($"DANH SÁCH TIẾN TRÌNH HỆ THỐNG - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    content.AppendLine($"Tổng số tiến trình: {currentProcesses.Count}");
                    content.AppendLine($"Tiến trình đáng ngờ: {currentProcesses.Count(p => p.IsSuspicious)}");
                    content.AppendLine();

                    foreach (var proc in currentProcesses)
                    {
                        content.AppendLine($"PID: {proc.Pid} | {proc.Name} | {proc.Description}");
                        if (proc.IsSuspicious)
                            content.AppendLine("  ⚠️ TIẾN TRÌNH ĐÁNG NGỜ");
                        content.AppendLine();
                    }

                    System.IO.File.WriteAllText(saveDialog.FileName, content.ToString());
                    AntdUI.Notification.success(this, "Thành công", "Đã xuất danh sách tiến trình", AntdUI.TAlignFrom.TR, Font);
                }
            }
            catch (Exception ex)
            {
                AntdUI.Modal.open(new AntdUI.Modal.Config(this, "Lỗi", $"Lỗi khi xuất danh sách: {ex.Message}")
                {
                    Icon = AntdUI.TType.Error,
                    OkText = "Đóng"
                });
            }
        }

        private void BtnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            LoadProcessData();
        }

        private void DgvProcesses_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.RowIndex < currentProcesses.Count)
            {
                var process = currentProcesses[e.RowIndex];
                ShowProcessDetails(process);
            }
        }

        private void DgvProcesses_SelectionChanged(object sender, EventArgs e)
        {
            // Optional: Show process details in status bar or tooltip
        }

        private void ShowProcessDetails(Proccess process)
        {
            var details = process.GetDetailedInfo();
            
            AntdUI.Modal.open(new AntdUI.Modal.Config(this, $"Chi tiết tiến trình - {process.Name}", details)
            {
                Icon = process.IsSuspicious ? AntdUI.TType.Warn : AntdUI.TType.Info,
                OkText = "Đóng",
                Width = 500
            });
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            refreshTimer?.Stop();
            refreshTimer?.Dispose();
            base.OnFormClosing(e);
        }
    }
}