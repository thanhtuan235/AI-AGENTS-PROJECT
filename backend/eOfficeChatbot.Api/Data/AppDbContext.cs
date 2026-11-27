using eOfficeChatbot.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace eOfficeChatbot.Api.Data
{
    public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ChatSession> ChatSessions { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
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