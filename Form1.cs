using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ComputerMonitoringClient.Views;

namespace ComputerMonitoringClient
{
    public partial class Form1 : Form
    {
        private TextBox txtExamCode;
        private TextBox txtRoomCode;
        private Button btnLogin;
        private Label lblTitle;
        private Label lblExamCode;
        private Label lblRoomCode;
        private Panel mainPanel;

        public Form1()
        {
            InitializeComponent();
            InitializeLoginUI();
        }

        private void InitializeLoginUI()
        {
            this.Text = "Đăng nhập hệ thống giám sát";
            this.Size = new Size(500, 400);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(240, 242, 245);

            // Main Panel
            mainPanel = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(500, 400),
                BackColor = Color.White
            };
            this.Controls.Add(mainPanel);

            // Title
            lblTitle = new Label
            {
                Text = "HỆ THỐNG GIÁM SÁT THI",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = Color.FromArgb(24, 144, 255),
                Location = new Point(50, 40),
                Size = new Size(400, 40),
                TextAlign = ContentAlignment.MiddleCenter
            };
            mainPanel.Controls.Add(lblTitle);

            // Label Mã dự thi
            lblExamCode = new Label
            {
                Text = "Mã dự thi:",
                Font = new Font("Segoe UI", 11),
                Location = new Point(80, 120),
                Size = new Size(100, 30),
                TextAlign = ContentAlignment.MiddleLeft
            };
            mainPanel.Controls.Add(lblExamCode);

            // Input Mã dự thi
            txtExamCode = new TextBox
            {
                Location = new Point(80, 150),
                Size = new Size(340, 35),
                Font = new Font("Segoe UI", 12),
                BorderStyle = BorderStyle.FixedSingle
            };
            mainPanel.Controls.Add(txtExamCode);

            // Label Mã phòng thi
            lblRoomCode = new Label
            {
                Text = "Mã phòng thi:",
                Font = new Font("Segoe UI", 11),
                Location = new Point(80, 200),
                Size = new Size(120, 30),
                TextAlign = ContentAlignment.MiddleLeft
            };
            mainPanel.Controls.Add(lblRoomCode);

            // Input Mã phòng thi
            txtRoomCode = new TextBox
            {
                Location = new Point(80, 230),
                Size = new Size(340, 35),
                Font = new Font("Segoe UI", 12),
                BorderStyle = BorderStyle.FixedSingle
            };
            mainPanel.Controls.Add(txtRoomCode);

            // Button Login
            btnLogin = new Button
            {
                Text = "Đăng nhập",
                Location = new Point(80, 300),
                Size = new Size(340, 45),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                BackColor = Color.FromArgb(24, 144, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Click += BtnLogin_Click;
            mainPanel.Controls.Add(btnLogin);
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            string examCode = txtExamCode.Text.Trim();
            string roomCode = txtRoomCode.Text.Trim();

            if (string.IsNullOrEmpty(examCode))
            {
                MessageBox.Show("Vui lòng nhập mã dự thi!", "Thông báo", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(roomCode))
            {
                MessageBox.Show("Vui lòng nhập mã phòng thi!", "Thông báo", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Đăng nhập thành công, mở form giám sát
            this.Hide();
            MonitoringForm monitoringForm = new MonitoringForm();
            monitoringForm.FormClosed += (s, args) => this.Close();
            monitoringForm.Show();
        }
    }
}
