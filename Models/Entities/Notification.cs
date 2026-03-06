using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JAS_MINE_IT15.Models.Entities
{
    /// <summary>
    /// Entity representing the Notifications table for persisting real-time notifications.
    /// </summary>
    [Table("Notifications")]
    public class Notification
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Target user for the notification.
        /// </summary>
        [Required]
        public int UserId { get; set; }

        /// <summary>
        /// Barangay scope for the notification (optional).
        /// </summary>
        public int? BarangayId { get; set; }

        /// <summary>
        /// Notification title.
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Notification message content.
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Notification type: pending, approval, rejected, announcement, urgent, discussion, info
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = "info";

        /// <summary>
        /// URL to navigate to when notification is clicked.
        /// </summary>
        [MaxLength(500)]
        public string? Link { get; set; }

        /// <summary>
        /// Related entity type: Document, Policy, Lesson, Practice, Announcement
        /// </summary>
        [MaxLength(50)]
        public string? RelatedEntityType { get; set; }

        /// <summary>
        /// ID of the related entity.
        /// </summary>
        public int? RelatedEntityId { get; set; }

        /// <summary>
        /// Whether the notification has been read by the user.
        /// </summary>
        public bool IsRead { get; set; } = false;

        /// <summary>
        /// When the notification was read.
        /// </summary>
        public DateTime? ReadAt { get; set; }

        /// <summary>
        /// Soft delete flag.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// When the notification was created.
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [ForeignKey("BarangayId")]
        public virtual Barangay? Barangay { get; set; }
    }
}
