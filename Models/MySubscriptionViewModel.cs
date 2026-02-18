using System.Collections.Generic;

namespace JAS_MINE_IT15.Models
{
    public class MySubscriptionViewModel
    {
        public string BarangayName { get; set; } = "Your Barangay";

        public SubscriptionSummary? Subscription { get; set; } = null;
        public List<PaymentRow> Payments { get; set; } = new();

        public class SubscriptionSummary
        {
            public string PlanName { get; set; } = "";
            public decimal Price { get; set; } = 0m;
            public string Status { get; set; } = ""; // Active | Expired | Pending
            public string StartDate { get; set; } = "";
            public string EndDate { get; set; } = "";
        }

        public class PaymentRow
        {
            public string Id { get; set; } = "";
            public decimal Amount { get; set; }
            public string Date { get; set; } = "";   // yyyy-MM-dd
            public string Method { get; set; } = "";
            public string Status { get; set; } = "Paid"; // Paid | Failed | Pending
        }
    }
}
