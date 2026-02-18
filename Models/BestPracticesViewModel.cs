using System.Collections.Generic;

namespace JAS_MINE_IT15.Models
{
    public class BestPracticesViewModel
    {
        public string SearchQuery { get; set; } = "";
        public string SelectedCategory { get; set; } = "";
        public bool CanManage { get; set; }

        // TODO: Load categories from database
        public List<string> Categories { get; set; } = new();

        public List<PracticeItem> Practices { get; set; } = new();
        public PracticeItem? FeaturedPractice { get; set; }

        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
