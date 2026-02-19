using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JAS_MINE_IT15.Models.Entities
{
    /// <summary>
    /// Entity representing the Announcements table.
    /// </summary>
    [Table("Announcements")]
    public class Announcement
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Allowed values: low, medium, high
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string Priority { get; set; } = "medium";

        /// <summary>
        /// Allowed values: draft, published, archived
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "draft";

        public bool IsPinned { get; set; } = false;

        public DateTime? PublishedAt { get; set; }

        public DateTime? ExpiresAt { get; set; }

        [Required]
        public int AuthorId { get; set; }

        public int? BarangayId { get; set; }

        [MaxLength(100)]
        public string? TargetAudience { get; set; }

        public int ViewCount { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("AuthorId")]
        public virtual User? Author { get; set; }
    }
}
