namespace eOfficeChatbot.Api.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = "Employee"; // Admin, Manager, Employee
        
        public int DepartmentId { get; set; }
        public Department? Department { get; set; }
    }
}