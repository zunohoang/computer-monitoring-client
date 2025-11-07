using System;
using ComputerMonitoringClient.Models;

namespace ComputerMonitoringClient.Services
{
    /// <summary>
    /// Service xử lý authentication và quản lý phiên đăng nhập
    /// </summary>
    public class AuthenticationService
    {
        private static AuthenticationService instance;
        private ExamSession currentSession;

        private AuthenticationService() { }

        public static AuthenticationService Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new AuthenticationService();
                }
                return instance;
            }
        }

        public ExamSession CurrentSession
        {
            get { return currentSession; }
        }

        /// <summary>
        /// Xác thực thông tin đăng nhập
        /// </summary>
        public bool Login(string examCode, string roomCode, out string errorMessage)
        {
            errorMessage = string.Empty;

            // Validate exam code
            if (string.IsNullOrWhiteSpace(examCode))
            {
                errorMessage = "Vui lòng nhập mã dự thi!";
                return false;
            }

            // Validate room code
            if (string.IsNullOrWhiteSpace(roomCode))
            {
                errorMessage = "Vui lòng nhập mã phòng thi!";
                return false;
            }

            // TODO: Thêm logic xác thực với server/database ở đây
            // Ví dụ: kiểm tra mã có tồn tại, kiểm tra thời gian thi, v.v.

            // Tạo phiên đăng nhập
            currentSession = new ExamSession(examCode, roomCode)
            {
                IsActive = true
            };

            return true;
        }

        /// <summary>
        /// Đăng xuất
        /// </summary>
        public void Logout()
        {
            if (currentSession != null)
            {
                currentSession.IsActive = false;
                currentSession = null;
            }
        }

        /// <summary>
        /// Kiểm tra trạng thái đăng nhập
        /// </summary>
        public bool IsLoggedIn()
        {
            return currentSession != null && currentSession.IsActive;
        }
    }
}
