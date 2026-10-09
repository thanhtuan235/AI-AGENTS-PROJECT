namespace eOfficeChatbot.Api.Models
{
    public class WorkflowStep
    {
        public int Id { get; set; }
        public int RequestId { get; set; }
        public string StepName { get; set; } = string.Empty;
        public string ApproverRole { get; set; } = string.Empty; // VD: "Manager", "HR"
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public Request? Request { get; set; }
    }
}