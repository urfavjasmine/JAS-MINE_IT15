using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JAS_MINE_IT15.Models.Entities
{
    /// <summary>
    /// Entity representing the KnowledgeDiscussions table.
    /// </summary>
    [Table("KnowledgeDiscussions")]
    public class KnowledgeDiscussion
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Category { get; set; }

        [Required]
        public int AuthorId { get; set; }

        public int? BarangayId { get; set; }

        public int LikesCount { get; set; } = 0;

        public int RepliesCount { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("AuthorId")]
        public virtual User? Author { get; set; }
    }
}
