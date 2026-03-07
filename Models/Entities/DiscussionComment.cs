using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JAS_MINE_IT15.Models.Entities
{
    /// <summary>
    /// Entity representing a comment on a knowledge discussion.
    /// </summary>
    [Table("DiscussionComments")]
    public class DiscussionComment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int DiscussionId { get; set; }

        [Required]
        public int AuthorId { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("DiscussionId")]
        public virtual KnowledgeDiscussion? Discussion { get; set; }

        [ForeignKey("AuthorId")]
        public virtual User? Author { get; set; }
    }
}
