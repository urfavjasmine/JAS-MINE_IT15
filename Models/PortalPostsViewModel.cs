using System;
using System.Collections.Generic;

namespace JAS_MINE_IT15.Models
{
    public class PortalPostsViewModel
    {
        // Permissions
        public bool CanManage { get; set; }
        public bool CanModify { get; set; }

        // Stats
        public int TotalPosts { get; set; }
        public int PinnedPosts { get; set; }
        public int RecentPosts { get; set; }

        // Filters
        public string SearchQuery { get; set; } = "";

        // Data
        public List<PortalPostItem> Posts { get; set; } = new();
        public PortalPostItem? SelectedPost { get; set; }

        // Messages
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class PortalPostItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public string Tags { get; set; } = "";
        public string PostedBy { get; set; } = "";
        public DateTime PostedOn { get; set; }
        public bool IsPinned { get; set; }
        public string BarangayName { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }
}
