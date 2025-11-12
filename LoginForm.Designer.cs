using System;
using System.Drawing;
using System.Windows.Forms;
using AntdUI;
using ComputerMonitoringClient.Utils;

namespace ComputerMonitoringClient.Views
{
    partial class LoginForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        private AntdUI.Input txtExamCode = null!;
        private AntdUI.Input txtRoomCode = null!;
        private AntdUI.Button btnLogin = null!;
        private AntdUI.Label lblTitle = null!;
        private AntdUI.Label lblExamCode = null!;
        private AntdUI.Label lblRoomCode = null!;
        private AntdUI.Panel mainPanel = null!;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.Text = "Đăng nhập hệ thống giám sát";
            this.Size = new Size(Utils.Constants.UI.LoginFormWidth, Utils.Constants.UI.LoginFormHeight);
            this.StartPosition = FormStartPosition.CenterScreen;
            //this.MaximizeBox = false;
            //this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.BackColor = Utils.Constants.Colors.Background;

            
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimizeBox = true;
            this.ControlBox = true;


            mainPanel = new AntdUI.Panel
            {
                Location = new Point(10, 10),
                Size = new Size(490, 390),
                Back = Utils.Constants.Colors.White,
                Shadow = Utils.Constants.UI.DefaultShadow,
                Radius = Utils.Constants.UI.DefaultRadius
            };
            this.Controls.Add(mainPanel);

            lblTitle = new AntdUI.Label
            {
                Text = "HỆ THỐNG GIÁM SÁT THI",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = Utils.Constants.Colors.Primary,
                Location = new Point(45, 40),
                Size = new Size(400, 40)
            };
            mainPanel.Controls.Add(lblTitle);

            lblExamCode = new AntdUI.Label
            {
                Text = "Mã dự thi",
                Font = new Font("Segoe UI", 11),
                ForeColor = Utils.Constants.Colors.TextPrimary,
                Location = new Point(80, 110),
                Size = new Size(100, 30)
            };
            mainPanel.Controls.Add(lblExamCode);

            txtExamCode = new AntdUI.Input
            {
                Location = new Point(80, 140),
                Size = new Size(330, 40),
                Font = new Font("Segoe UI", 11),
                PlaceholderText = "Nhập mã dự thi"
            };
            mainPanel.Controls.Add(txtExamCode);

            lblRoomCode = new AntdUI.Label
            {
                Text = "Mã phòng thi",
                Font = new Font("Segoe UI", 11),
                ForeColor = Utils.Constants.Colors.TextPrimary,
                Location = new Point(80, 200),
                Size = new Size(120, 30)
            };
            mainPanel.Controls.Add(lblRoomCode);

            txtRoomCode = new AntdUI.Input
            {
                Location = new Point(80, 230),
                Size = new Size(330, 40),
                Font = new Font("Segoe UI", 11),
                PlaceholderText = "Nhập mã phòng thi"
            };
            txtRoomCode.KeyPress += TxtRoomCode_KeyPress;
            mainPanel.Controls.Add(txtRoomCode);

            btnLogin = new AntdUI.Button
            {
                Text = "Đăng nhập",
                Location = new Point(80, 300),
                Size = new Size(330, 45),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Type = AntdUI.TTypeMini.Primary,
                BorderWidth = 0f,
                Radius = Utils.Constants.UI.DefaultRadius
            };
            btnLogin.Click += BtnLogin_Click;
            mainPanel.Controls.Add(btnLogin);
        }

        #endregion
    }
}
