using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JAS_MINE_IT15.Models.Entities
{
    [Table("Invoices")]
    public class Invoice
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string InvoiceNumber { get; set; } = string.Empty;

        [Required]
        public int SubscriptionId { get; set; }

        public int? BarangayId { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }

        [Column(TypeName = "date")]
        public DateTime? DueDate { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Unpaid"; // Unpaid, Paid, Overdue, Void

        public DateTime IssuedAt { get; set; } = DateTime.Now;
        public DateTime? PaidAt { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey("SubscriptionId")]
        public virtual BarangaySubscription? Subscription { get; set; }

        [ForeignKey("BarangayId")]
        public virtual Barangay? Barangay { get; set; }

        public virtual ICollection<SubscriptionPayment>? Payments { get; set; }
    }
}
