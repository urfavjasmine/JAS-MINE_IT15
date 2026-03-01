using System.Collections.Generic;

namespace JAS_MINE_IT15.Models
{
    public class MySubscriptionViewModel
    {
        public string BarangayName { get; set; } = "Your Barangay";

        public SubscriptionSummary? Subscription { get; set; } = null;
        public List<PaymentRow> Payments { get; set; } = new();
        public List<InvoiceRow> Invoices { get; set; } = new();

        /// <summary>True when redirected from subscription gate with expired=true.</summary>
        public bool ShowExpiredWarning { get; set; }

        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

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
            public string Status { get; set; } = "Paid"; // Paid | Failed | Pending | PendingVerification | Approved | Rejected
            public string Reference { get; set; } = "";
            public string? ProofUrl { get; set; }
            public string? RejectionReason { get; set; }
        }

        public class InvoiceRow
        {
            public int Id { get; set; }
            public string InvoiceNumber { get; set; } = "";
            public decimal Amount { get; set; }
            public string Status { get; set; } = "Unpaid";
            public string IssuedAt { get; set; } = "";
            public string? DueDate { get; set; }
        }
    }

    /// <summary>
    /// ViewModel for the SelectPlan page (barangay_admin picks a plan).
    /// </summary>
    public class SelectPlanViewModel
    {
        public List<AvailablePlan> Plans { get; set; } = new();
        public bool HasActiveSubscription { get; set; }
        public bool HasBarangay { get; set; }
        public int? SelectedPlanId { get; set; }
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        public class AvailablePlan
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string Description { get; set; } = "";
            public decimal Price { get; set; }
            public int DurationMonths { get; set; }
            public string? Features { get; set; }
        }
    }

    /// <summary>
    /// ViewModel for the SuperAdmin PendingPayments verification page.
    /// </summary>
    public class PendingPaymentsViewModel
    {
        public List<PendingPaymentRow> Payments { get; set; } = new();
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        public class PendingPaymentRow
        {
            public int PaymentId { get; set; }
            public string BarangayName { get; set; } = "";
            public string PlanName { get; set; } = "";
            public string InvoiceNumber { get; set; } = "";
            public decimal Amount { get; set; }
            public string PaymentDate { get; set; } = "";
            public string PaymentMethod { get; set; } = "";
            public string? ProofUrl { get; set; }
            public string? ReferenceNumber { get; set; }
        }
    }
}
