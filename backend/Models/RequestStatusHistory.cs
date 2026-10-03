namespace eOfficeChatbot.Api.Models
{
    public class RequestStatusHistory
    {
        public int Id { get; set; }
        public int RequestId { get; set; }
        public int ChangedByUserId { get; set; } // Ai là người chuyển trạng thái
        public string OldStatus { get; set; } = string.Empty;
        public string NewStatus { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty; // Lời phê (nếu từ chối)
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        public Request? Request { get; set; }
    }
}