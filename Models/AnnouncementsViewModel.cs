using System.Collections.Generic;
using System.Linq;

namespace JAS_MINE_IT15.Models
{
    public class AnnouncementItem
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public string Priority { get; set; } = "medium"; // high/medium/low
        public string Status { get; set; } = "draft";    // published/draft
        public string Date { get; set; } = "";           // yyyy-MM-dd
        public string Author { get; set; } = "";
        public int Views { get; set; }
        public bool Pinned { get; set; }
        public bool IsArchived { get; set; }
    }

    public class AnnouncementsViewModel
    {
        public List<AnnouncementItem> Announcements { get; set; } = new();
        public string Filter { get; set; } = "all";
        public string ArchiveStatus { get; set; } = "active";

        // Permission flags
        public bool CanCreate { get; set; }
        public bool CanArchive { get; set; }

        // Stats
        public int Total { get; set; }
        public int Published { get; set; }
        public int Drafts { get; set; }
        public int Pinned { get; set; }
        public int Archived { get; set; }

        // Messages
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
