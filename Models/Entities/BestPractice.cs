using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JAS_MINE_IT15.Models.Entities
{
    /// <summary>
    /// Entity representing the BestPractices table.
    /// </summary>
    [Table("BestPractices")]
    public class BestPractice
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Purpose of the best practice
        /// </summary>
        public string? Purpose { get; set; }

        /// <summary>
        /// Steps to implement the best practice
        /// </summary>
        [Required]
        public string Steps { get; set; } = string.Empty;

        /// <summary>
        /// Resources needed for implementation
        /// </summary>
        public string? ResourcesNeeded { get; set; }

        /// <summary>
        /// Office responsible for this practice
        /// </summary>
        [MaxLength(200)]
        public string? OwnerOffice { get; set; }

        /// <summary>
        /// Status: Active or Archived
        /// </summary>
        [MaxLength(20)]
        public string Status { get; set; } = "Active";

        [Required]
        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        public int? BarangayId { get; set; }

        [MaxLength(150)]
        public string? BarangayName { get; set; }

        [Column(TypeName = "decimal(3,2)")]
        public decimal Rating { get; set; } = 0.00m;

        public int Implementations { get; set; } = 0;

        public bool IsFeatured { get; set; } = false;

        [MaxLength(500)]
        public string? AttachmentUrl { get; set; }

        [Required]
        public int SubmittedById { get; set; }

        public int? ApprovedById { get; set; }

        public DateTime? ApprovedAt { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Soft delete flag for archive functionality
        /// </summary>
        public bool IsArchived { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("SubmittedById")]
        public virtual User? SubmittedBy { get; set; }

        [ForeignKey("ApprovedById")]
        public virtual User? ApprovedBy { get; set; }
    }
}
