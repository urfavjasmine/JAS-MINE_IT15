using System.Collections.Generic;

namespace JAS_MINE_IT15.Models
{
    public class BestPracticesViewModel
    {
        public string SearchQuery { get; set; } = "";
        public string SelectedCategory { get; set; } = "All Categories";
        public bool CanManage { get; set; }

        public List<string> Categories { get; set; } = new()
        {
            "All Categories","Health","Education","Governance","Environment","Safety","Finance"
        };

        public List<PracticeItem> Practices { get; set; } = new();
        public PracticeItem? FeaturedPractice { get; set; }

        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        // optional helper for CSS mapping in view
        public HashSet<string> CategoryCss { get; set; } = new()
        {
            "cat-Health","cat-Education","cat-Governance","cat-Environment","cat-Safety","cat-Finance"
        };
    }
}
