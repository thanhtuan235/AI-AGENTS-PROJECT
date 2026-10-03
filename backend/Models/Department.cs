namespace eOfficeChatbot.Api.Models
{
    public class Department
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? ManagerId { get; set; } // Người trưởng phòng (sẽ duyệt đơn)
        
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}