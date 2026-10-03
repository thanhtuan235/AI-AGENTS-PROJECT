namespace eOfficeChatbot.Api.Models
{
    public class Document
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty; // VD: "NhanSu", "HanhChinh"
        public string ContentUrl { get; set; } = string.Empty; // Link file PDF/Word
        public string Summary { get; set; } = string.Empty; // Tóm tắt để LLM đọc nhanh
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}