namespace JAS_MINE_IT15.Models.Enums
{
    /// <summary>
    /// Standard document/policy lifecycle statuses.
    /// Maps to existing DB free-text: "draft", "pending", "approved", "rejected", "archived"
    /// </summary>
    public enum ContentStatus
    {
        Draft,
        Pending,
        Approved,
        Rejected,
        Archived
    }

    /// <summary>
    /// Announcement priority levels.
    /// Maps to existing: "low", "medium", "high"
    /// </summary>
    public enum PriorityLevel
    {
        Low,
        Medium,
        High
    }

    /// <summary>
    /// Subscription statuses.
    /// Maps to existing: "Active", "Expired", "Cancelled", "Pending"
    /// </summary>
    public enum SubscriptionStatus
    {
        Active,
        Expired,
        Cancelled,
        Pending
    }

    /// <summary>
    /// Invoice statuses.
    /// </summary>
    public enum InvoiceStatus
    {
        Unpaid,
        Paid,
        Overdue,
        Void
    }

    /// <summary>
    /// Payment processing statuses.
    /// </summary>
    public enum PaymentStatus
    {
        Pending,
        PendingVerification,
        Approved,
        Rejected,
        Paid,
        Failed,
        Refunded
    }

    /// <summary>
    /// User roles in the system.
    /// </summary>
    public enum UserRole
    {
        super_admin,
        barangay_admin,
        barangay_secretary,
        barangay_staff,
        council_member
    }

    /// <summary>
    /// Helper to convert enums ↔ DB strings (backward compatible).
    /// Existing data uses lowercase strings; these helpers keep parity.
    /// </summary>
    public static class StatusHelper
    {
        public static string ToDbString(this ContentStatus s) => s switch
        {
            ContentStatus.Draft => "draft",
            ContentStatus.Pending => "pending",
            ContentStatus.Approved => "approved",
            ContentStatus.Rejected => "rejected",
            ContentStatus.Archived => "archived",
            _ => "draft"
        };

        public static ContentStatus ToContentStatus(string? s) => (s ?? "").ToLower() switch
        {
            "draft" => ContentStatus.Draft,
            "pending" => ContentStatus.Pending,
            "approved" => ContentStatus.Approved,
            "rejected" => ContentStatus.Rejected,
            "archived" => ContentStatus.Archived,
            _ => ContentStatus.Draft
        };

        public static string ToDbString(this PriorityLevel p) => p switch
        {
            PriorityLevel.Low => "low",
            PriorityLevel.Medium => "medium",
            PriorityLevel.High => "high",
            _ => "medium"
        };

        public static PriorityLevel ToPriorityLevel(string? s) => (s ?? "").ToLower() switch
        {
            "low" => PriorityLevel.Low,
            "medium" => PriorityLevel.Medium,
            "high" => PriorityLevel.High,
            _ => PriorityLevel.Medium
        };
    }
}
