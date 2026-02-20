using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JAS_MINE_IT15.Models.Entities
{
    /// <summary>
    /// Entity representing the LessonsLearned table.
    /// </summary>
    [Table("LessonsLearned")]
    public class LessonLearned
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Summary { get; set; } = string.Empty;

        /// <summary>
        /// Problem description for CRUD
        /// </summary>
        [Required]
        public string Problem { get; set; } = string.Empty;

        /// <summary>
        /// Action taken to address the problem
        /// </summary>
        [Required]
        public string ActionTaken { get; set; } = string.Empty;

        /// <summary>
        /// Result of the action taken
        /// </summary>
        [Required]
        public string Result { get; set; } = string.Empty;

        /// <summary>
        /// Recommendation for future reference
        /// </summary>
        public string? Recommendation { get; set; }

        /// <summary>
        /// Date when the lesson was recorded
        /// </summary>
        public DateTime DateRecorded { get; set; } = DateTime.Now;

        [MaxLength(200)]
        public string? ProjectName { get; set; }

        [MaxLength(100)]
        public string? ProjectType { get; set; }

        [MaxLength(500)]
        public string? Tags { get; set; }

        /// <summary>
        /// Allowed values: draft, pending, approved, rejected
        /// </summary>
        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "pending";

        [Required]
        public int SubmittedById { get; set; }

        public int? ApprovedById { get; set; }

        public DateTime? ApprovedAt { get; set; }

        public int? BarangayId { get; set; }

        /// <summary>
        /// Soft delete flag
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        public int LikesCount { get; set; } = 0;

        public int CommentsCount { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("SubmittedById")]
        public virtual User? SubmittedBy { get; set; }

        [ForeignKey("ApprovedById")]
        public virtual User? ApprovedBy { get; set; }
    }
}
