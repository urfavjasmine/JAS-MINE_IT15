using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JAS_MINE_IT15.Models.Entities
{
    /// <summary>
    /// Entity representing the SubscriptionPayments table.
    /// </summary>
    [Table("SubscriptionPayments")]
    public class SubscriptionPayment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int SubscriptionId { get; set; }

        public int? InvoiceId { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }

        [Required]
        [Column(TypeName = "date")]
        public DateTime PaymentDate { get; set; }

        /// <summary>
        /// Examples: GCash, Bank Transfer, Cash, Maya
        /// </summary>
        [MaxLength(50)]
        public string? PaymentMethod { get; set; }

        [MaxLength(100)]
        public string? ReferenceNumber { get; set; }

        /// <summary>
        /// URL to uploaded proof-of-payment image/PDF.
        /// </summary>
        [MaxLength(500)]
        public string? ProofOfPaymentUrl { get; set; }

        /// <summary>
        /// Allowed values: Pending, PendingVerification, Approved, Rejected, Paid, Failed, Refunded
        /// </summary>
        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "Pending";

        /// <summary>
        /// Reason provided by Super Admin when rejecting a payment.
        /// </summary>
        [MaxLength(500)]
        public string? RejectionReason { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        public int? ProcessedById { get; set; }
        public DateTime? ProcessedAt { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("SubscriptionId")]
        public virtual BarangaySubscription? Subscription { get; set; }

        [ForeignKey("InvoiceId")]
        public virtual Invoice? Invoice { get; set; }

        [ForeignKey("ProcessedById")]
        public virtual User? ProcessedBy { get; set; }
    }
}
