using System.Collections.Generic;
using System.Linq;

namespace JAS_MINE_IT15.Models
{
    public class DiscussionItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public string? Category { get; set; }
        public int AuthorId { get; set; }
        public string AuthorName { get; set; } = "";
        public int? BarangayId { get; set; }
        public string? BarangayName { get; set; }
        public int LikesCount { get; set; }
        public int RepliesCount { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class KnowledgeDiscussionsViewModel
    {
        public List<DiscussionItem> Discussions { get; set; } = new();
        public string SearchQuery { get; set; } = "";
        public string CategoryFilter { get; set; } = "All Categories";

        // Stats
        public int TotalDiscussions => Discussions.Count;
        public int TotalLikes => Discussions.Sum(d => d.LikesCount);
        public int TotalReplies => Discussions.Sum(d => d.RepliesCount);

        // Categories for filter dropdown
        public List<string> Categories { get; set; } = new()
        {
            "All Categories",
            "General",
            "Technical",
            "Policy",
            "Best Practices",
            "Q&A",
            "Announcements"
        };

        // For Create/Edit modal
        public DiscussionItem? EditingDiscussion { get; set; }
        public bool IsEditMode { get; set; }

        // Permission flags
        public bool CanCreate { get; set; }
        public bool CanEdit { get; set; }
        public bool CanArchive { get; set; }
    }
}
