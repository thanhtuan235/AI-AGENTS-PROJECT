using eOfficeChatbot.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace eOfficeChatbot.Api.Data
{
    public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Department> Departments { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Document> Documents { get; set; }
    public DbSet<FormTemplate> FormTemplates { get; set; }
    public DbSet<Request> Requests { get; set; }
    public DbSet<RequestStatusHistory> RequestStatusHistories { get; set; }
    public DbSet<ChatSession> ChatSessions { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
    public DbSet<WorkflowStep> WorkflowSteps { get; set; }
    public DbSet<Feedback> Feedbacks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Tắt xóa dây chuyền từ ChatSession -> Feedback
            modelBuilder.Entity<Feedback>()
                .HasOne(f => f.ChatSession)
                .WithMany()
                .HasForeignKey(f => f.ChatSessionId)
                .OnDelete(DeleteBehavior.Restrict);

            // Tắt xóa dây chuyền từ ChatMessage -> Feedback
            modelBuilder.Entity<Feedback>()
                .HasOne(f => f.Message)
                .WithMany()
                .HasForeignKey(f => f.MessageId)
                .OnDelete(DeleteBehavior.Restrict);
        }
}
}