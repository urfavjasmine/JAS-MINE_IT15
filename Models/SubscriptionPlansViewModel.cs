using System.Collections.Generic;

namespace JAS_MINE_IT15.Models
{
    public class SubscriptionPlansViewModel
    {
        public string SearchQuery { get; set; } = "";
        public List<PlanItem> Plans { get; set; } = new();

        public int TotalPlans { get; set; }
        public int ActivePlans { get; set; }
        public int InactivePlans { get; set; }
        public int YearlyPlans { get; set; }

        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
