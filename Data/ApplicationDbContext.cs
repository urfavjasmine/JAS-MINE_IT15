using JAS_MINE_IT15.Models.Entities;
using JAS_MINE_IT15.Services;
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
        public DbSet<Invoice> Invoices { get; set; } = null!;
        public DbSet<KnowledgeDiscussion> KnowledgeDiscussions { get; set; } = null!;
        public DbSet<PasswordResetRequest> PasswordResetRequests { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<DiscussionLike> DiscussionLikes { get; set; } = null!;
        public DbSet<DiscussionComment> DiscussionComments { get; set; } = null!;

            public override int SaveChanges()
            {
                  ApplyAuditLogHashChain();
                  return base.SaveChanges();
            }

            public override int SaveChanges(bool acceptAllChangesOnSuccess)
            {
                  ApplyAuditLogHashChain();
                  return base.SaveChanges(acceptAllChangesOnSuccess);
            }

            public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            {
                  await ApplyAuditLogHashChainAsync(cancellationToken);
                  return await base.SaveChangesAsync(cancellationToken);
            }

            public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
            {
                  await ApplyAuditLogHashChainAsync(cancellationToken);
                  return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            }

            private void ApplyAuditLogHashChain()
            {
                  var pendingLogs = ChangeTracker.Entries<AuditLog>()
                        .Where(e => e.State == EntityState.Added)
                        .Select(e => e.Entity)
                        .OrderBy(e => e.CreatedAt)
                        .ThenBy(e => e.Action)
                        .ToList();

                  if (!pendingLogs.Any())
                  {
                        return;
                  }

                  var previousHash = AuditLogs.AsNoTracking()
                        .OrderByDescending(l => l.Id)
                        .Select(l => l.Hash)
                        .FirstOrDefault();

                  foreach (var pendingLog in pendingLogs)
                  {
                        pendingLog.PreviousHash = string.IsNullOrWhiteSpace(previousHash) ? null : previousHash;
                        pendingLog.Hash = AuditLogIntegrity.ComputeHash(pendingLog, pendingLog.PreviousHash);
                        previousHash = pendingLog.Hash;
                  }
            }

            private async Task ApplyAuditLogHashChainAsync(CancellationToken cancellationToken)
            {
                  var pendingLogs = ChangeTracker.Entries<AuditLog>()
                        .Where(e => e.State == EntityState.Added)
                        .Select(e => e.Entity)
                        .OrderBy(e => e.CreatedAt)
                        .ThenBy(e => e.Action)
                        .ToList();

                  if (!pendingLogs.Any())
                  {
                        return;
                  }

                  var previousHash = await AuditLogs.AsNoTracking()
                        .OrderByDescending(l => l.Id)
                        .Select(l => l.Hash)
                        .FirstOrDefaultAsync(cancellationToken);

                  foreach (var pendingLog in pendingLogs)
                  {
                        pendingLog.PreviousHash = string.IsNullOrWhiteSpace(previousHash) ? null : previousHash;
                        pendingLog.Hash = AuditLogIntegrity.ComputeHash(pendingLog, pendingLog.PreviousHash);
                        previousHash = pendingLog.Hash;
                  }
            }

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
                        entity.Property(e => e.PreviousHash).HasMaxLength(64);
                entity.Property(e => e.Hash).HasMaxLength(64);
                entity.HasIndex(e => e.Hash)
                      .IsUnique()
                      .HasFilter("[Hash] IS NOT NULL");

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
                entity.Property(e => e.DurationMonths).HasDefaultValue(1);
                entity.Property(e => e.UserLimit).HasDefaultValue(4);
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
            // Invoice entity configuration
            // =============================================
            builder.Entity<Invoice>(entity =>
            {
                entity.HasIndex(e => e.InvoiceNumber).IsUnique();
                entity.Property(e => e.Status).HasDefaultValue("Unpaid");
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.Subscription)
                      .WithMany()
                      .HasForeignKey(e => e.SubscriptionId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Barangay)
                      .WithMany()
                      .HasForeignKey(e => e.BarangayId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // =============================================
            // SubscriptionPayment – add Invoice FK
            // =============================================
            builder.Entity<SubscriptionPayment>(entity =>
            {
                entity.HasOne(e => e.Invoice)
                      .WithMany(i => i.Payments)
                      .HasForeignKey(e => e.InvoiceId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // =============================================
            // Notification entity configuration
            // =============================================
            builder.Entity<Notification>(entity =>
            {
                entity.Property(e => e.Type).HasDefaultValue("info");
                entity.Property(e => e.IsRead).HasDefaultValue(false);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Barangay)
                      .WithMany()
                      .HasForeignKey(e => e.BarangayId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.UserId, e.IsRead, e.IsActive });
                entity.HasIndex(e => e.CreatedAt);
            });

            // =============================================
            // DiscussionLike entity configuration
            // =============================================
            builder.Entity<DiscussionLike>(entity =>
            {
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.Discussion)
                      .WithMany()
                      .HasForeignKey(e => e.DiscussionId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Unique constraint: one like per user per discussion
                entity.HasIndex(e => new { e.DiscussionId, e.UserId }).IsUnique();
            });

            // =============================================
            // DiscussionComment entity configuration
            // =============================================
            builder.Entity<DiscussionComment>(entity =>
            {
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.Discussion)
                      .WithMany()
                      .HasForeignKey(e => e.DiscussionId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Author)
                      .WithMany()
                      .HasForeignKey(e => e.AuthorId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.DiscussionId);
            });
        }
    }
}
