using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JAS_MINE_IT15.Models.Entities
{
    /// <summary>
    /// Entity representing the KnowledgeRepository table.
    /// </summary>
    [Table("KnowledgeRepository")]
    public class KnowledgeDocument
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Tags { get; set; }

        [MaxLength(500)]
        public string? FileUrl { get; set; }

        [MaxLength(255)]
        public string? FileName { get; set; }

        public long? FileSize { get; set; }

        [MaxLength(50)]
        public string? FileType { get; set; }

        /// <summary>
        /// Allowed values: draft, pending, approved, rejected
        /// </summary>
        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "pending";

        [Required]
        [MaxLength(20)]
        public string Version { get; set; } = "1.0";

        [Required]
        public int UploadedById { get; set; }

        public int? ApprovedById { get; set; }

        public DateTime? ApprovedAt { get; set; }

        public int? BarangayId { get; set; }

        public int ViewCount { get; set; } = 0;

        public int DownloadCount { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Soft delete flag for archive functionality
        /// </summary>
        public bool IsArchived { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("UploadedById")]
        public virtual User? UploadedBy { get; set; }

        [ForeignKey("ApprovedById")]
        public virtual User? ApprovedBy { get; set; }
    }
}
