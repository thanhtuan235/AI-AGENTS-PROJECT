namespace eOfficeChatbot.Api.Models
{
    public class FormTemplate
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty; // VD: "Leave", "Purchase", "RoomBooking"
        public string DownloadUrl { get; set; } = string.Empty;
    }
}