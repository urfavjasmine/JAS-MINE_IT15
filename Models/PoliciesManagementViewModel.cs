using System.Collections.Generic;

namespace JAS_MINE_IT15.Models
{
    public class PoliciesManagementViewModel
    {
        // filters (for UI)
        public string StatusFilter { get; set; } = "all";
        public string SearchQuery { get; set; } = "";
        public string ArchiveStatus { get; set; } = "active";

        // permissions
        public bool CanCreate { get; set; }
        public bool CanApprove { get; set; }
        public bool CanArchive { get; set; }

        // counts
        public int CountAll { get; set; }
        public int CountApproved { get; set; }
        public int CountPending { get; set; }
        public int CountDraft { get; set; }
        public int CountArchived { get; set; }

        // list
        public List<PolicyItem> Policies { get; set; } = new();
    }
}
