using EOffice.Mock.Models;
using Microsoft.EntityFrameworkCore;

namespace EOffice.Mock.Data
{
    public class EOfficeMockDbContext : DbContext
    {
        public EOfficeMockDbContext(DbContextOptions<EOfficeMockDbContext> options) : base(options) { }

        // M1 – Người dùng & phân quyền
        public DbSet<Department> Departments => Set<Department>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<Permission> Permissions => Set<Permission>();
        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
        public DbSet<User> Users => Set<User>();

        // M2 – Kho tri thức (Metadata)
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Document> Documents => Set<Document>();
        public DbSet<DocumentRoleAccess> DocumentRoleAccesses => Set<DocumentRoleAccess>();

        // M4 – Yêu cầu & workflow
        public DbSet<RequestType> RequestTypes => Set<RequestType>();
        public DbSet<FormTemplate> FormTemplates => Set<FormTemplate>();
        public DbSet<WorkflowStep> WorkflowSteps => Set<WorkflowStep>();
        public DbSet<Request> Requests => Set<Request>();
        public DbSet<RequestApproval> RequestApprovals => Set<RequestApproval>();
        public DbSet<RequestStatusHistory> RequestStatusHistories => Set<RequestStatusHistory>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =========================================================
            // M1 – NGƯỜI DÙNG & PHÂN QUYỀN
            // =========================================================
            modelBuilder.Entity<Department>(entity =>
            {
                entity.HasIndex(d => d.DepartmentCode).IsUnique();
                entity.HasIndex(d => d.HeadUserID).IsUnique();

                entity.HasOne(d => d.HeadUser)
                      .WithOne()
                      .HasForeignKey<Department>(d => d.HeadUserID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasIndex(r => r.RoleCode).IsUnique();
            });

            modelBuilder.Entity<Permission>(entity =>
            {
                entity.HasIndex(p => p.PermissionCode).IsUnique();
            });

            modelBuilder.Entity<RolePermission>(entity =>
            {
                entity.HasKey(rp => new { rp.RoleID, rp.PermissionID });

                entity.HasOne(rp => rp.Role)
                      .WithMany(r => r.RolePermissions)
                      .HasForeignKey(rp => rp.RoleID)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(rp => rp.Permission)
                      .WithMany(p => p.RolePermissions)
                      .HasForeignKey(rp => rp.PermissionID)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Username).IsUnique();
                entity.HasIndex(u => u.Email).IsUnique();

                entity.HasOne(u => u.Department)
                      .WithMany(d => d.Users)
                      .HasForeignKey(u => u.DepartmentID)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(u => u.Role)
                      .WithMany(r => r.Users)
                      .HasForeignKey(u => u.RoleID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // =========================================================
            // M2 – KHO TRI THỨC (METADATA)
            // =========================================================
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasIndex(c => c.CategoryName).IsUnique();
            });

            modelBuilder.Entity<Document>(entity =>
            {
                entity.HasIndex(d => d.SupersedesDocumentID).IsUnique();

                entity.HasOne(d => d.Category)
                      .WithMany(c => c.Documents)
                      .HasForeignKey(d => d.CategoryID)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.IssuedByDepartment)
                      .WithMany(dept => dept.IssuedDocuments)
                      .HasForeignKey(d => d.IssuedByDepartmentID)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.UploadedByUser)
                      .WithMany(u => u.UploadedDocuments)
                      .HasForeignKey(d => d.UploadedByUserID)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.SupersedesDocument)
                      .WithOne()
                      .HasForeignKey<Document>(d => d.SupersedesDocumentID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<DocumentRoleAccess>(entity =>
            {
                entity.HasKey(dra => new { dra.DocumentID, dra.RoleID });

                entity.HasOne(dra => dra.Document)
                      .WithMany(d => d.DocumentRoleAccesses)
                      .HasForeignKey(dra => dra.DocumentID)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(dra => dra.Role)
                      .WithMany(r => r.DocumentRoleAccesses)
                      .HasForeignKey(dra => dra.RoleID)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // =========================================================
            // M4 – YÊU CẦU & WORKFLOW
            // =========================================================
            modelBuilder.Entity<RequestType>(entity =>
            {
                entity.HasIndex(rt => rt.TypeCode).IsUnique();
            });

            modelBuilder.Entity<FormTemplate>(entity =>
            {
                entity.HasOne(ft => ft.RequestType)
                      .WithMany(rt => rt.FormTemplates)
                      .HasForeignKey(ft => ft.RequestTypeID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<WorkflowStep>(entity =>
            {
                entity.HasIndex(ws => new { ws.RequestTypeID, ws.StepOrder }).IsUnique();

                // Thực thể yếu của RequestType -> Cascade
                entity.HasOne(ws => ws.RequestType)
                      .WithMany(rt => rt.WorkflowSteps)
                      .HasForeignKey(ws => ws.RequestTypeID)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ws => ws.ApproverRole)
                      .WithMany(r => r.WorkflowSteps)
                      .HasForeignKey(ws => ws.ApproverRoleID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Request>(entity =>
            {
                entity.HasIndex(r => r.IdempotencyKey).IsUnique();

                entity.HasOne(r => r.User)
                      .WithMany(u => u.Requests)
                      .HasForeignKey(r => r.UserID)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.RequestType)
                      .WithMany(rt => rt.Requests)
                      .HasForeignKey(r => r.RequestTypeID)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.FormTemplate)
                      .WithMany(ft => ft.Requests)
                      .HasForeignKey(r => r.TemplateID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<RequestApproval>(entity =>
            {
                entity.HasIndex(ra => new { ra.RequestID, ra.StepOrder }).IsUnique();

                // Thực thể yếu của Request -> Cascade
                entity.HasOne(ra => ra.Request)
                      .WithMany(r => r.Approvals)
                      .HasForeignKey(ra => ra.RequestID)
                      .OnDelete(DeleteBehavior.Cascade);

                // Tránh multiple cascade path
                entity.HasOne(ra => ra.WorkflowStep)
                      .WithMany(ws => ws.RequestApprovals)
                      .HasForeignKey(ra => ra.WorkflowStepID)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(ra => ra.ActedByUser)
                      .WithMany(u => u.ActedApprovals)
                      .HasForeignKey(ra => ra.ActedByUserID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<RequestStatusHistory>(entity =>
            {
                entity.HasIndex(rsh => new { rsh.RequestID, rsh.ChangeNo }).IsUnique();

                // Thực thể yếu của Request -> Cascade
                entity.HasOne(rsh => rsh.Request)
                      .WithMany(r => r.StatusHistories)
                      .HasForeignKey(rsh => rsh.RequestID)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(rsh => rsh.ChangedByUser)
                      .WithMany(u => u.StatusChanges)
                      .HasForeignKey(rsh => rsh.ChangedByUserID)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}