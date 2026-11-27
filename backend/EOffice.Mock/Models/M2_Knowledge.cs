using System.ComponentModel.DataAnnotations;

namespace EOffice.Mock.Models
{
    public class Category
    {
        [Key]
        public int CategoryID { get; set; }

        [Required, MaxLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public ICollection<Document> Documents { get; set; } = new List<Document>();
    }

    public class Document
    {
        [Key]
        public int DocumentID { get; set; }

        [Required, MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        public string? Summary { get; set; }

        [Required, MaxLength(500)]
        public string FileUrl { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string Version { get; set; } = "1.0";

        public DateTime EffectiveDate { get; set; }

        public DateTime? ExpiryDate { get; set; }

        [Required, MaxLength(50)]
        public string AccessLevel { get; set; } = "Internal"; // Public, Internal, Restricted

        [Required, MaxLength(50)]
        public string Status { get; set; } = "Active"; // Active, Superseded, Expired

        [Required, MaxLength(50)]
        public string IndexStatus { get; set; } = "Pending"; // Pending, Indexed, Failed

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int CategoryID { get; set; }
        public Category? Category { get; set; }

        public int IssuedByDepartmentID { get; set; }
        public Department? IssuedByDepartment { get; set; }

        public int UploadedByUserID { get; set; }
        public User? UploadedByUser { get; set; }

        public int? SupersedesDocumentID { get; set; }
        public Document? SupersedesDocument { get; set; }

        public ICollection<DocumentRoleAccess> DocumentRoleAccesses { get; set; } = new List<DocumentRoleAccess>();
    }

    public class DocumentRoleAccess
    {
        public int DocumentID { get; set; }
        public Document? Document { get; set; }

        public int RoleID { get; set; }
        public Role? Role { get; set; }
    }
}