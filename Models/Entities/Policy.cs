using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JAS_MINE_IT15.Models.Entities
{
    /// <summary>
    /// Entity representing the Policies table.
    /// </summary>
    [Table("Policies")]
    public class Policy
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? Content { get; set; }

        [MaxLength(100)]
        public string? Category { get; set; }

        /// <summary>
        /// Allowed values: draft, pending, approved, rejected, archived
        /// </summary>
        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "draft";

        [Required]
        [MaxLength(20)]
        public string Version { get; set; } = "1.0";

        [Column(TypeName = "date")]
        public DateTime? EffectiveDate { get; set; }

        [Column(TypeName = "date")]
        public DateTime? ExpiryDate { get; set; }

        [Required]
        public int AuthorId { get; set; }

        public int? ApprovedById { get; set; }

        public DateTime? ApprovedAt { get; set; }

        public int? BarangayId { get; set; }

        [MaxLength(500)]
        public string? AttachmentUrl { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Soft delete flag for archive functionality
        /// </summary>
        public bool IsArchived { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("AuthorId")]
        public virtual User? Author { get; set; }

        [ForeignKey("ApprovedById")]
        public virtual User? ApprovedBy { get; set; }
    }
}
