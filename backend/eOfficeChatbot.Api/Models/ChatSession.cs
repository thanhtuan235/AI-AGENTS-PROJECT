namespace eOfficeChatbot.Api.Models
{
    public class ChatSession
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = "New Conversation";
        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        
        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }
}