using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EOffice.Mock.Models
{
    public class Department
    {
        [Key]
        public int DepartmentID { get; set; }

        [Required, MaxLength(50)]
        public string DepartmentCode { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string DepartmentName { get; set; } = string.Empty;

        public int? HeadUserID { get; set; }
        public User? HeadUser { get; set; }

        public ICollection<User> Users { get; set; } = new List<User>();
        public ICollection<Document> IssuedDocuments { get; set; } = new List<Document>();
    }

    public class Role
    {
        [Key]
        public int RoleID { get; set; }

        [Required, MaxLength(50)]
        public string RoleCode { get; set; } = string.Empty; // Employee, Manager, Admin

        [Required, MaxLength(100)]
        public string RoleName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public ICollection<User> Users { get; set; } = new List<User>();
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
        public ICollection<DocumentRoleAccess> DocumentRoleAccesses { get; set; } = new List<DocumentRoleAccess>();
        public ICollection<WorkflowStep> WorkflowSteps { get; set; } = new List<WorkflowStep>();
    }

    public class Permission
    {
        [Key]
        public int PermissionID { get; set; }

        [Required, MaxLength(100)]
        public string PermissionCode { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }

    public class RolePermission
    {
        public int RoleID { get; set; }
        public Role? Role { get; set; }

        public int PermissionID { get; set; }
        public Permission? Permission { get; set; }
    }

    public class User
    {
        [Key]
        public int UserID { get; set; }

        [Required, MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [NotMapped]
        public string FullName => $"{LastName} {FirstName}".Trim();

        [Required, MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [MaxLength(20)]
        public string? Gender { get; set; }

        [MaxLength(255)]
        public string? PasswordHash { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? LastSyncedAt { get; set; }

        public int DepartmentID { get; set; }
        public Department? Department { get; set; }

        public int RoleID { get; set; }
        public Role? Role { get; set; }

        public ICollection<Request> Requests { get; set; } = new List<Request>();
        public ICollection<Document> UploadedDocuments { get; set; } = new List<Document>();
        public ICollection<RequestApproval> ActedApprovals { get; set; } = new List<RequestApproval>();
        public ICollection<RequestStatusHistory> StatusChanges { get; set; } = new List<RequestStatusHistory>();
    }
}