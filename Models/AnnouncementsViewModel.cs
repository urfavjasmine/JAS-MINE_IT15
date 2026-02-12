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
    }

    public class AnnouncementsViewModel
    {
        public List<AnnouncementItem> Announcements { get; set; } = new();
        public string Filter { get; set; } = "all";

        public int Total => Announcements.Count;
        public int Published => Announcements.Count(a => a.Status == "published");
        public int Drafts => Announcements.Count(a => a.Status == "draft");
        public int Pinned => Announcements.Count(a => a.Pinned);
    }
}
