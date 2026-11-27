namespace eOfficeChatbot.Api.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public int ChatSessionId { get; set; }
        public string SenderRole { get; set; } = "User"; // "User", "Bot", "System"
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public ChatSession? ChatSession { get; set; }
    }
}