using System.Collections.Generic;

namespace JAS_MINE_IT15.Models
{
    public class SubscriptionPaymentsViewModel
    {
        public string SearchQuery { get; set; } = "";
        public string StatusFilter { get; set; } = "";

        public List<PaymentItem> Payments { get; set; } = new();

        public int TotalPayments { get; set; }
        public decimal TotalCollected { get; set; }
        public int PendingCount { get; set; }
        public int PendingVerificationCount { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }
        public int FailedCount { get; set; }

        public List<string> Barangays { get; set; } = new();
        public List<string> Plans { get; set; } = new();
        public List<string> Methods { get; set; } = new();

        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
