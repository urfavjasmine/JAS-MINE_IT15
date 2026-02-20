using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JAS_MINE_IT15.Models.Entities
{
    /// <summary>
    /// Entity representing the Ordinances table.
    /// </summary>
    [Table("Ordinances")]
    public class Ordinance
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrdinanceId { get; set; }

        [Required]
        [MaxLength(50)]
        public string OrdinanceNo { get; set; } = string.Empty;

        [Required]
        public int SeriesYear { get; set; }

        [Required]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        public string? Summary { get; set; }

        public DateTime? DateApproved { get; set; }

        public DateTime? EffectiveDate { get; set; }

        [MaxLength(100)]
        public string? Category { get; set; }

        [Required]
        public int BarangayId { get; set; }

        /// <summary>
        /// Allowed values: Active, Archived
        /// </summary>
        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "Active";

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        // Navigation property
        [ForeignKey("BarangayId")]
        public virtual Barangay? Barangay { get; set; }
    }
}
