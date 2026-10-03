namespace eOfficeChatbot.Api.Models
{
    public class Request
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string RequestType { get; set; } = string.Empty; // "Leave", "Purchase", "RoomBooking"
        public string Title { get; set; } = string.Empty;
        
        // Chứa chi tiết dạng chuỗi JSON. 
        // VD Nghỉ phép: {"StartDate":"2023-10-01", "EndDate":"2023-10-02", "Reason":"Ốm"}
        public string PayloadDetail { get; set; } = string.Empty; 
        
        public string Status { get; set; } = "Pending"; // Pending, ManagerApproved, Rejected, Completed
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User? User { get; set; }
        public ICollection<RequestStatusHistory> StatusHistories { get; set; } = new List<RequestStatusHistory>();
    }
}