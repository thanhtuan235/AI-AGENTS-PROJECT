namespace eOfficeChatbot.Api.DTOs
{
    public class LeaveRequestDto
    {
        public int UserId { get; set; }
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }
}