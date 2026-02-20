using System;
using System.Collections.Generic;
using System.Linq;

namespace JAS_MINE_IT15.Models
{
    public class OrdinanceItem
    {
        public int OrdinanceId { get; set; }
        public string OrdinanceNo { get; set; } = "";
        public int SeriesYear { get; set; }
        public string Title { get; set; } = "";
        public string? Summary { get; set; }
        public DateTime? DateApproved { get; set; }
        public DateTime? EffectiveDate { get; set; }
        public string? Category { get; set; }
        public int BarangayId { get; set; }
        public string? BarangayName { get; set; }
        public string Status { get; set; } = "Active";
        public DateTime CreatedAt { get; set; }
    }

    public class OrdinancesViewModel
    {
        public List<OrdinanceItem> Ordinances { get; set; } = new();
        public string SearchQuery { get; set; } = "";
        public string StatusFilter { get; set; } = "all";
        public int? YearFilter { get; set; }

        // Stats
        public int TotalOrdinances => Ordinances.Count;
        public int ActiveOrdinances => Ordinances.Count(o => o.Status == "Active");
        public int ArchivedOrdinances => Ordinances.Count(o => o.Status == "Archived");

        // Available years for filter dropdown
        public List<int> AvailableYears { get; set; } = new();

        // Categories for dropdown
        public List<string> Categories { get; set; } = new()
        {
            "General",
            "Administrative",
            "Fiscal",
            "Environmental",
            "Health & Sanitation",
            "Public Safety",
            "Zoning",
            "Social Welfare"
        };

        // Permission flags
        public bool CanCreate { get; set; }
        public bool CanEdit { get; set; }
        public bool CanArchive { get; set; }

        // Messages
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
