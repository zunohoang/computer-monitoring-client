using System;

namespace ComputerMonitoringClient.Models
{
    /// <summary>
    /// Model đại diện cho phiên thi
    /// </summary>
    public class ExamSession
    {
        public string ExamCode { get; set; }
        public string RoomCode { get; set; }
        public DateTime LoginTime { get; set; }
        public bool IsActive { get; set; }

        public ExamSession()
        {
            LoginTime = DateTime.Now;
            IsActive = false;
        }

        public ExamSession(string examCode, string roomCode)
        {
            ExamCode = examCode;
            RoomCode = roomCode;
            LoginTime = DateTime.Now;
            IsActive = false;
        }

        public override string ToString()
        {
            return $"Mã dự thi: {ExamCode} - Phòng thi: {RoomCode}";
        }
    }
}
