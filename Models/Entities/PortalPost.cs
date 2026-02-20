using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JAS_MINE_IT15.Models.Entities
{
    /// <summary>
    /// Entity representing portal posts/articles.
    /// </summary>
    [Table("PortalPosts")]
    public class PortalPost
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PortalPostId { get; set; }

        [Required]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Tags { get; set; }

        [MaxLength(150)]
        public string? PostedBy { get; set; }

        public DateTime PostedOn { get; set; } = DateTime.Now;

        public int? BarangayId { get; set; }

        public bool IsPinned { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Soft delete flag
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        // Navigation properties
        [ForeignKey("BarangayId")]
        public virtual Barangay? Barangay { get; set; }
    }
}
