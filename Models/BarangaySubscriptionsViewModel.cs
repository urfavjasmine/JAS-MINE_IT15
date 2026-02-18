using System.Collections.Generic;

namespace JAS_MINE_IT15.Models
{
    public class BarangaySubscriptionsViewModel
    {
        public string SearchQuery { get; set; } = "";
        public string StatusFilter { get; set; } = "all";

        public List<SubscriptionItem> Subscriptions { get; set; } = new();

        public int TotalCount { get; set; }
        public int ActiveCount { get; set; }
        public int ExpiredCount { get; set; }
        public int CancelledCount { get; set; }

        public List<string> Barangays { get; set; } = new();
        public List<string> Plans { get; set; } = new();

        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
