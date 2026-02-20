using System.Collections.Generic;
using System.Linq;

namespace JAS_MINE_IT15.Models
{
    public class BarangayItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? Code { get; set; }
        public string? Municipality { get; set; }
        public string? Province { get; set; }
        public string? Region { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public string? Address { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class BarangaysManagementViewModel
    {
        public List<BarangayItem> Barangays { get; set; } = new();
        public string SearchQuery { get; set; } = "";

        // Stats
        public int TotalBarangays => Barangays.Count;
        public int ActiveBarangays => Barangays.Count(b => b.IsActive);
        public int InactiveBarangays => Barangays.Count(b => !b.IsActive);

        // For Create/Edit modal
        public BarangayItem? EditingBarangay { get; set; }
        public bool IsEditMode { get; set; }

        // Permission flags
        public bool CanCreate { get; set; }
        public bool CanEdit { get; set; }
        public bool CanArchive { get; set; }
    }
}
