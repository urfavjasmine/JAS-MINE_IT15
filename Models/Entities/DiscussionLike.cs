using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JAS_MINE_IT15.Models.Entities
{
    /// <summary>
    /// Entity representing a like on a knowledge discussion.
    /// </summary>
    [Table("DiscussionLikes")]
    public class DiscussionLike
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int DiscussionId { get; set; }

        [Required]
        public int UserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("DiscussionId")]
        public virtual KnowledgeDiscussion? Discussion { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}
