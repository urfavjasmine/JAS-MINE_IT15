using JAS_MINE_IT15.Models;

namespace JAS_MINE_IT15.Services
{
    // ─── DTOs ───

    public class BarangayReportSummary
    {
        public int BarangayId { get; set; }
        public string BarangayName { get; set; } = "";
        public int TotalUsers { get; set; }
        public int TotalDocuments { get; set; }
        public int TotalPolicies { get; set; }
        public int TotalLessons { get; set; }
        public int TotalBestPractices { get; set; }
        public int TotalDiscussions { get; set; }
        public int TotalAnnouncements { get; set; }
        public string SubscriptionStatus { get; set; } = "None";
        public string PlanName { get; set; } = "—";
        public DateTime? SubscriptionExpiry { get; set; }
    }

    public class UserActivityRow
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Role { get; set; } = "";
        public string? BarangayName { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public int LoginCount { get; set; }
        public int DocumentsCreated { get; set; }
        public int PoliciesCreated { get; set; }
        public int LessonsCreated { get; set; }
        public int DiscussionsCreated { get; set; }
        public int TotalContributions { get; set; }
    }

    public class ContentLifecycleRow
    {
        public string Module { get; set; } = "";
        public int Draft { get; set; }
        public int Pending { get; set; }
        public int Approved { get; set; }
        public int Rejected { get; set; }
        public int Archived { get; set; }
        public int Total { get; set; }
    }

    public class TimeSeriesPoint
    {
        public string Label { get; set; } = "";
        public int Count { get; set; }
    }

    // ─── Interface ───

    /// <summary>
    /// Aggregated reporting across all modules.
    /// All methods respect tenant isolation via ITenantService.
    /// </summary>
    public interface IReportingService
    {
        /// <summary>Per-barangay drill-down summary (super_admin).</summary>
        Task<List<BarangayReportSummary>> GetBarangaySummariesAsync();

        /// <summary>Single barangay detail.</summary>
        Task<BarangayReportSummary?> GetBarangayDetailAsync(int barangayId);

        /// <summary>User activity report with date-range filter.</summary>
        Task<PagedResult<UserActivityRow>> GetUserActivityAsync(
            DateTime? from = null, DateTime? to = null,
            string? search = null, int page = 1, int pageSize = 20);

        /// <summary>Document/policy lifecycle (status distribution) filtered by date range.</summary>
        Task<List<ContentLifecycleRow>> GetContentLifecycleAsync(DateTime? from = null, DateTime? to = null);

        /// <summary>Content creation over time (monthly).</summary>
        Task<List<TimeSeriesPoint>> GetContentTimelineAsync(string module, int months = 12);

        /// <summary>Overall system counts for report header cards.</summary>
        Task<ReportDashboardCounts> GetDashboardCountsAsync();
    }

    public class ReportDashboardCounts
    {
        public int TotalBarangays { get; set; }
        public int TotalUsers { get; set; }
        public int TotalDocuments { get; set; }
        public int TotalPolicies { get; set; }
        public int ActiveSubscriptions { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
