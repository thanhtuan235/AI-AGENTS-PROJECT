using System.ComponentModel.DataAnnotations;

namespace EOffice.Mock.Models
{
    public class RequestType
    {
        [Key]
        public int RequestTypeID { get; set; }

        [Required, MaxLength(50)]
        public string TypeCode { get; set; } = string.Empty; // Leave, Purchase, RoomBooking

        [Required, MaxLength(150)]
        public string TypeName { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public ICollection<FormTemplate> FormTemplates { get; set; } = new List<FormTemplate>();
        public ICollection<WorkflowStep> WorkflowSteps { get; set; } = new List<WorkflowStep>();
        public ICollection<Request> Requests { get; set; } = new List<Request>();
    }

    public class FormTemplate
    {
        [Key]
        public int TemplateID { get; set; }

        public int RequestTypeID { get; set; }
        public RequestType? RequestType { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string Version { get; set; } = "1.0";

        [Required]
        public string FieldsJson { get; set; } = "[]";

        [MaxLength(500)]
        public string? DownloadUrl { get; set; }

        public string? Instructions { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<Request> Requests { get; set; } = new List<Request>();
    }

    public class WorkflowStep
    {
        [Key]
        public int WorkflowStepID { get; set; }

        public int RequestTypeID { get; set; }
        public RequestType? RequestType { get; set; }

        public int StepOrder { get; set; }

        [Required, MaxLength(150)]
        public string StepName { get; set; } = string.Empty;

        public int ApproverRoleID { get; set; }
        public Role? ApproverRole { get; set; }

        [Required, MaxLength(50)]
        public string ApproverScope { get; set; } = "SameDepartment"; // SameDepartment, SpecificDepartment, Global

        public ICollection<RequestApproval> RequestApprovals { get; set; } = new List<RequestApproval>();
    }

    public class Request
    {
        [Key]
        public int RequestID { get; set; }

        [Required, MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Payload { get; set; } = "{}"; // JSON string

        [Required, MaxLength(50)]
        public string CurrentStatus { get; set; } = "Pending"; // Pending, Approved, Rejected, Cancelled

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Required, MaxLength(100)]
        public string IdempotencyKey { get; set; } = Guid.NewGuid().ToString();

        public int UserID { get; set; }
        public User? User { get; set; }

        public int RequestTypeID { get; set; }
        public RequestType? RequestType { get; set; }

        public int TemplateID { get; set; }
        public FormTemplate? FormTemplate { get; set; }

        public ICollection<RequestApproval> Approvals { get; set; } = new List<RequestApproval>();
        public ICollection<RequestStatusHistory> StatusHistories { get; set; } = new List<RequestStatusHistory>();
    }

    public class RequestApproval
    {
        [Key]
        public int ApprovalID { get; set; }

        public int RequestID { get; set; }
        public Request? Request { get; set; }

        public int StepOrder { get; set; }

        public int WorkflowStepID { get; set; }
        public WorkflowStep? WorkflowStep { get; set; }

        [Required, MaxLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected, Skipped

        public DateTime? ActedAt { get; set; }

        public string? Comment { get; set; }

        public int? ActedByUserID { get; set; }
        public User? ActedByUser { get; set; }
    }

    public class RequestStatusHistory
    {
        [Key]
        public int ChangeID { get; set; }

        public int RequestID { get; set; }
        public Request? Request { get; set; }

        public int ChangeNo { get; set; }

        [MaxLength(50)]
        public string? OldStatus { get; set; }

        [Required, MaxLength(50)]
        public string NewStatus { get; set; } = string.Empty;

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        public string? Note { get; set; }

        public int ChangedByUserID { get; set; }
        public User? ChangedByUser { get; set; }
    }
}