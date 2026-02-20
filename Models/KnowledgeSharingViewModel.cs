using System.Collections.Generic;

namespace JAS_MINE_IT15.Models
{
    public class KnowledgeSharingViewModel
    {
        public bool CanPost { get; set; }
        public bool CanAnnounce { get; set; }
        public bool CanArchive { get; set; }

        public string ArchiveStatus { get; set; } = "active";
        public string SearchQuery { get; set; } = "";
        public string SelectedCategory { get; set; } = "All Categories";

        public List<KnowledgeDiscussionItem> Discussions { get; set; } = new();
        public List<KnowledgeAnnouncementItem> Announcements { get; set; } = new();
        public List<KnowledgeSharedDocItem> SharedDocuments { get; set; } = new();

        public List<string> ActiveMembers { get; set; } = new();
        public List<string> Categories { get; set; } = new();
        public int MembersOnline { get; set; }

        // Stats
        public int TotalDiscussions { get; set; }
        public int ArchivedDiscussions { get; set; }

        // Messages
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class KnowledgeDiscussionItem
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public string Author { get; set; } = "";
        public string Avatar { get; set; } = "";
        public string Date { get; set; } = "";
        public string Category { get; set; } = "";
        public int Replies { get; set; }
        public int Likes { get; set; }
        public bool IsArchived { get; set; }
    }

    public class KnowledgeAnnouncementItem
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public string Author { get; set; } = "";
        public string Date { get; set; } = "";
        public bool Pinned { get; set; }
        public int Likes { get; set; }
        public int Comments { get; set; }
    }

    public class KnowledgeSharedDocItem
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string SharedBy { get; set; } = "";
        public string Date { get; set; } = "";
        public int Downloads { get; set; }
    }
}
