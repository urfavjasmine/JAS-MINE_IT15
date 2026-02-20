using System;
using System.Collections.Generic;

namespace JAS_MINE_IT15.Models
{
    public class BestPracticesViewModel
    {
        public string SearchQuery { get; set; } = "";
        public string SelectedCategory { get; set; } = "";
        public string SelectedStatus { get; set; } = "";
        public bool CanManage { get; set; }
        public bool CanModify { get; set; }

        // Stats
        public int TotalPractices { get; set; }
        public int ActivePractices { get; set; }
        public int ArchivedPractices { get; set; }

        // TODO: Load categories from database
        public List<string> Categories { get; set; } = new();

        public List<BestPracticeItem> Practices { get; set; } = new();
        public BestPracticeItem? FeaturedPractice { get; set; }

        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class BestPracticeItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Purpose { get; set; } = "";
        public string Steps { get; set; } = "";
        public string ResourcesNeeded { get; set; } = "";
        public string OwnerOffice { get; set; } = "";
        public string Category { get; set; } = "";
        public string Status { get; set; } = "";
        public string Barangay { get; set; } = "";
        public string DateAdded { get; set; } = "";
        public decimal Rating { get; set; }
        public int Implementations { get; set; }
        public string SubmittedBy { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public bool IsFeatured { get; set; }
        public bool Featured => IsFeatured;
    }
}
