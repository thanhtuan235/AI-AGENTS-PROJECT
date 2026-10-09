namespace eOfficeChatbot.Api.Models
{
    public class Feedback
    {
        public int Id { get; set; }
        public int ChatSessionId { get; set; }
        public int MessageId { get; set; }
        public int Rating { get; set; } // VD: 1 (Like), -1 (Dislike)
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ChatSession? ChatSession { get; set; }
        public ChatMessage? Message { get; set; }
    }
}