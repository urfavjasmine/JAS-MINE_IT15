using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JAS_MINE_IT15.Models.Entities
{
    /// <summary>
    /// Entity representing the AuditLogs table.
    /// </summary>
    [Table("AuditLogs")]
    public class AuditLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public int? UserId { get; set; }

        [MaxLength(255)]
        public string? UserEmail { get; set; }

        [MaxLength(150)]
        public string? UserName { get; set; }

        /// <summary>
        /// Examples: Created, Updated, Deleted, Approved, Rejected, Login, Logout
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// Examples: KnowledgeRepository, Policies, Users
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Module { get; set; } = string.Empty;

        public int? TargetId { get; set; }

        [MaxLength(100)]
        public string? TargetType { get; set; }

        [MaxLength(300)]
        public string? TargetName { get; set; }

        public string? Description { get; set; }

        public string? OldValues { get; set; }

        public string? NewValues { get; set; }

        [MaxLength(45)]
        public string? IpAddress { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }

        [MaxLength(100)]
        public string? SessionId { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}
