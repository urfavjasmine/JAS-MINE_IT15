using JAS_MINE_IT15.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace JAS_MINE_IT15.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // =============================================
        // Business Entity DbSets (14 tables from SQL schema)
        // =============================================
        public DbSet<User> BusinessUsers { get; set; } = null!;
        public DbSet<Barangay> Barangays { get; set; } = null!;
        public DbSet<KnowledgeDocument> KnowledgeDocuments { get; set; } = null!;
        public DbSet<Policy> Policies { get; set; } = null!;
        public DbSet<LessonLearned> LessonsLearned { get; set; } = null!;
        public DbSet<BestPractice> BestPractices { get; set; } = null!;
        public DbSet<Announcement> Announcements { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; } = null!;
        public DbSet<BarangaySubscription> BarangaySubscriptions { get; set; } = null!;
        public DbSet<SubscriptionPayment> SubscriptionPayments { get; set; } = null!;
        public DbSet<KnowledgeDiscussion> KnowledgeDiscussions { get; set; } = null!;
        public DbSet<PasswordResetRequest> PasswordResetRequests { get; set; } = null!;
        public DbSet<SharedDocument> SharedDocuments { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // =============================================
            // User entity configuration
            // =============================================
            builder.Entity<User>(entity =>
            {
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            });

            // =============================================
            // Barangay entity configuration
            // =============================================
            builder.Entity<Barangay>(entity =>
            {
                entity.HasIndex(e => e.Name).IsUnique();
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            });

            // =============================================
            // KnowledgeDocument entity configuration
            // =============================================
            builder.Entity<KnowledgeDocument>(entity =>
            {
                entity.Property(e => e.Status).HasDefaultValue("pending");
                entity.Property(e => e.Version).HasDefaultValue("1.0");
                entity.Property(e => e.ViewCount).HasDefaultValue(0);
                entity.Property(e => e.DownloadCount).HasDefaultValue(0);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.UploadedBy)
                      .WithMany()
                      .HasForeignKey(e => e.UploadedById)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.ApprovedBy)
                      .WithMany()
                      .HasForeignKey(e => e.ApprovedById)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // =============================================
            // Policy entity configuration
            // =============================================
            builder.Entity<Policy>(entity =>
            {
                entity.Property(e => e.Status).HasDefaultValue("draft");
                entity.Property(e => e.Version).HasDefaultValue("1.0");
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.Author)
                      .WithMany()
                      .HasForeignKey(e => e.AuthorId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.ApprovedBy)
                      .WithMany()
                      .HasForeignKey(e => e.ApprovedById)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // =============================================
            // LessonLearned entity configuration
            // =============================================
            builder.Entity<LessonLearned>(entity =>
            {
                entity.Property(e => e.Status).HasDefaultValue("pending");
                entity.Property(e => e.LikesCount).HasDefaultValue(0);
                entity.Property(e => e.CommentsCount).HasDefaultValue(0);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.SubmittedBy)
                      .WithMany()
                      .HasForeignKey(e => e.SubmittedById)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.ApprovedBy)
                      .WithMany()
                      .HasForeignKey(e => e.ApprovedById)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // =============================================
            // BestPractice entity configuration
            // =============================================
            builder.Entity<BestPractice>(entity =>
            {
                entity.Property(e => e.Rating).HasDefaultValue(0.00m);
                entity.Property(e => e.Implementations).HasDefaultValue(0);
                entity.Property(e => e.IsFeatured).HasDefaultValue(false);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.SubmittedBy)
                      .WithMany()
                      .HasForeignKey(e => e.SubmittedById)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.ApprovedBy)
                      .WithMany()
                      .HasForeignKey(e => e.ApprovedById)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // =============================================
            // Announcement entity configuration
            // =============================================
            builder.Entity<Announcement>(entity =>
            {
                entity.Property(e => e.Priority).HasDefaultValue("medium");
                entity.Property(e => e.Status).HasDefaultValue("draft");
                entity.Property(e => e.IsPinned).HasDefaultValue(false);
                entity.Property(e => e.ViewCount).HasDefaultValue(0);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.Author)
                      .WithMany()
                      .HasForeignKey(e => e.AuthorId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // =============================================
            // AuditLog entity configuration
            // =============================================
            builder.Entity<AuditLog>(entity =>
            {
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // =============================================
            // SubscriptionPlan entity configuration
            // =============================================
            builder.Entity<SubscriptionPlan>(entity =>
            {
                entity.Property(e => e.Price).HasDefaultValue(0.00m);
                entity.Property(e => e.DurationMonths).HasDefaultValue(12);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            });

            // =============================================
            // BarangaySubscription entity configuration
            // =============================================
            builder.Entity<BarangaySubscription>(entity =>
            {
                entity.Property(e => e.Status).HasDefaultValue("Active");
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.Barangay)
                      .WithMany()
                      .HasForeignKey(e => e.BarangayId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Plan)
                      .WithMany()
                      .HasForeignKey(e => e.PlanId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // =============================================
            // SubscriptionPayment entity configuration
            // =============================================
            builder.Entity<SubscriptionPayment>(entity =>
            {
                entity.Property(e => e.Status).HasDefaultValue("Pending");
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.Subscription)
                      .WithMany()
                      .HasForeignKey(e => e.SubscriptionId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.ProcessedBy)
                      .WithMany()
                      .HasForeignKey(e => e.ProcessedById)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // =============================================
            // KnowledgeDiscussion entity configuration
            // =============================================
            builder.Entity<KnowledgeDiscussion>(entity =>
            {
                entity.Property(e => e.LikesCount).HasDefaultValue(0);
                entity.Property(e => e.RepliesCount).HasDefaultValue(0);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.Author)
                      .WithMany()
                      .HasForeignKey(e => e.AuthorId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // =============================================
            // PasswordResetRequest entity configuration
            // =============================================
            builder.Entity<PasswordResetRequest>(entity =>
            {
                entity.Property(e => e.Status).HasDefaultValue("Pending");
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.ProcessedBy)
                      .WithMany()
                      .HasForeignKey(e => e.ProcessedById)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // =============================================
            // SharedDocument entity configuration
            // =============================================
            builder.Entity<SharedDocument>(entity =>
            {
                entity.Property(e => e.DownloadCount).HasDefaultValue(0);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.SharedBy)
                      .WithMany()
                      .HasForeignKey(e => e.SharedById)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
