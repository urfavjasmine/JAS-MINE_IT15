using System;

namespace JAS_MINE_IT15.Models
{
    public class SubscriptionItem
    {
        public string Id { get; set; } = "";
        public string BarangayName { get; set; } = "";
        public string PlanName { get; set; } = "";
        public string StartDate { get; set; } = ""; // "yyyy-MM-dd"
        public string EndDate { get; set; } = "";   // "yyyy-MM-dd"
        public string Status { get; set; } = "Active"; // Active | Expired | Cancelled
    }
}
