namespace eOfficeChatbot.Api.DTOs
{
    public class FeedbackDto
    {
        public int ChatSessionId { get; set; }
        public int MessageId { get; set; }
        public int Rating { get; set; } // 1 (Like), -1 (Dislike)
        public string Comment { get; set; } = string.Empty;
    }
}