using JAS_MINE_IT15.Filters;
using JAS_MINE_IT15.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace JAS_MINE_IT15.Controllers
{
    /// <summary>
    /// Reporting & analytics — per-barangay drill-down, user activity,
    /// content lifecycle, revenue trends, subscription churn + CSV export.
    /// </summary>
    public class ReportsController : Controller
    {
        private readonly IReportingService _reports;
        private readonly ISubscriptionService _subscriptions;
        private readonly IAuditService _audit;

        public ReportsController(
            IReportingService reports,
            ISubscriptionService subscriptions,
            IAuditService audit)
        {
            _reports = reports;
            _subscriptions = subscriptions;
            _audit = audit;
        }

        // ─── helpers ───
        private string Role => HttpContext.Session.GetString("Role") ?? "";
        private bool IsSuperAdmin => Role == "super_admin";

        // ══════════════════════════════════════════════
        //  1.  Reports Overview (dashboard cards + links)
        // ══════════════════════════════════════════════
        [RequireRoles("super_admin", "barangay_admin")]
        public async Task<IActionResult> Index()
        {
            var counts = await _reports.GetDashboardCountsAsync();
            return View(counts);
        }

        // ══════════════════════════════════════════════
        //  2.  Per-Barangay Drill-Down  (super_admin only)
        // ══════════════════════════════════════════════
        [RequireRoles("super_admin")]
        public async Task<IActionResult> BarangaySummary()
        {
            var list = await _reports.GetBarangaySummariesAsync();
            return View(list);
        }

        [RequireRoles("super_admin")]
        public async Task<IActionResult> BarangayDetail(int id)
        {
            var detail = await _reports.GetBarangayDetailAsync(id);
            if (detail == null) return NotFound();
            return View(detail);
        }

        // ══════════════════════════════════════════════
        //  3.  User Activity
        // ══════════════════════════════════════════════
        [RequireRoles("super_admin", "barangay_admin")]
        public async Task<IActionResult> UserActivity(
            DateTime? from, DateTime? to, string? search, string? range, int page = 1)
        {
            // Handle range shortcuts
            if (!string.IsNullOrEmpty(range))
            {
                to = DateTime.Today;
                from = range switch
                {
                    "7" => DateTime.Today.AddDays(-7),
                    "30" => DateTime.Today.AddDays(-30),
                    "thismonth" => new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1),
                    "90" => DateTime.Today.AddDays(-90),
                    "180" => DateTime.Today.AddDays(-180),
                    "365" => DateTime.Today.AddDays(-365),
                    _ => DateTime.Today.AddDays(-30)
                };
            }

            var result = await _reports.GetUserActivityAsync(from, to, search, page);

            ViewData["FromDate"] = from?.ToString("yyyy-MM-dd");
            ViewData["ToDate"] = to?.ToString("yyyy-MM-dd");
            ViewData["Search"] = search;
            ViewData["Range"] = range;
            ViewData["TotalPages"] = result.TotalPages;
            ViewData["CurrentPage"] = result.Page;
            ViewData["PaginationUrl"] = Url.Action("UserActivity", new { from = from?.ToString("yyyy-MM-dd"), to = to?.ToString("yyyy-MM-dd"), search });

            return View(result);
        }

        // ══════════════════════════════════════════════
        //  4.  Content Lifecycle
        // ══════════════════════════════════════════════
        [RequireRoles("super_admin", "barangay_admin")]
        public async Task<IActionResult> ContentLifecycle(DateTime? from, DateTime? to)
        {
            var rows = await _reports.GetContentLifecycleAsync(from, to);

            ViewData["FromDate"] = from?.ToString("yyyy-MM-dd");
            ViewData["ToDate"] = to?.ToString("yyyy-MM-dd");

            return View(rows);
        }

        // ══════════════════════════════════════════════
        //  5.  Revenue Trends  (super_admin)
        // ══════════════════════════════════════════════
        [RequireRoles("super_admin")]
        public async Task<IActionResult> RevenueTrends()
        {
            var timeline = await _subscriptions.GetRevenueTimelineAsync(12);
            var distribution = await _subscriptions.GetPlanDistributionAsync();

            ViewData["PlanDistribution"] = distribution;
            return View(timeline);
        }

        // ══════════════════════════════════════════════
        //  6.  Content Timeline API (for Chart.js AJAX)
        // ══════════════════════════════════════════════
        [HttpGet]
        [RequireRoles("super_admin", "barangay_admin")]
        public async Task<IActionResult> ContentTimelineData(string module = "documents", int months = 12)
        {
            var data = await _reports.GetContentTimelineAsync(module, months);
            return Json(new
            {
                labels = data.Select(d => d.Label),
                values = data.Select(d => d.Count)
            });
        }

        // ══════════════════════════════════════════════
        //  CSV EXPORTS
        // ══════════════════════════════════════════════

        [RequireRoles("super_admin")]
        [HttpGet]
        public async Task<IActionResult> ExportBarangaySummaryCsv()
        {
            var list = await _reports.GetBarangaySummariesAsync();
            var sb = new StringBuilder();
            sb.AppendLine("Barangay,Users,Documents,Policies,Lessons,Best Practices,Discussions,Announcements,Subscription,Plan,Expiry");
            foreach (var r in list)
            {
                sb.AppendLine($"\"{r.BarangayName}\",{r.TotalUsers},{r.TotalDocuments},{r.TotalPolicies},{r.TotalLessons},{r.TotalBestPractices},{r.TotalDiscussions},{r.TotalAnnouncements},\"{r.SubscriptionStatus}\",\"{r.PlanName}\",{r.SubscriptionExpiry?.ToString("yyyy-MM-dd") ?? ""}");
            }

            await _audit.LogAsync("Export", "Reports", null, "BarangaySummary", null, "Exported barangay summary CSV");
            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"BarangaySummary_{DateTime.Now:yyyyMMdd}.csv");
        }

        [RequireRoles("super_admin", "barangay_admin")]
        [HttpGet]
        public async Task<IActionResult> ExportUserActivityCsv(DateTime? from, DateTime? to)
        {
            var result = await _reports.GetUserActivityAsync(from, to, null, 1, 10000);
            var sb = new StringBuilder();
            sb.AppendLine("Name,Email,Role,Barangay,Last Login,Logins,Documents,Policies,Lessons,Discussions,Total");
            foreach (var r in result.Items)
            {
                sb.AppendLine($"\"{r.FullName}\",\"{r.Email}\",\"{r.Role}\",\"{r.BarangayName}\",{r.LastLoginAt?.ToString("yyyy-MM-dd HH:mm") ?? ""},{ r.LoginCount},{r.DocumentsCreated},{r.PoliciesCreated},{r.LessonsCreated},{r.DiscussionsCreated},{r.TotalContributions}");
            }

            await _audit.LogAsync("Export", "Reports", null, "UserActivity", null, "Exported user activity CSV");
            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"UserActivity_{DateTime.Now:yyyyMMdd}.csv");
        }

        [RequireRoles("super_admin", "barangay_admin")]
        [HttpGet]
        public async Task<IActionResult> ExportContentLifecycleCsv(DateTime? from, DateTime? to)
        {
            var rows = await _reports.GetContentLifecycleAsync(from, to);
            var sb = new StringBuilder();
            sb.AppendLine("Module,Draft,Pending,Approved,Rejected,Archived,Total");
            foreach (var r in rows)
            {
                sb.AppendLine($"\"{r.Module}\",{r.Draft},{r.Pending},{r.Approved},{r.Rejected},{r.Archived},{r.Total}");
            }

            await _audit.LogAsync("Export", "Reports", null, "ContentLifecycle", null, "Exported content lifecycle CSV");
            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"ContentLifecycle_{DateTime.Now:yyyyMMdd}.csv");
        }

        [RequireRoles("super_admin")]
        [HttpGet]
        public async Task<IActionResult> ExportRevenueCsv()
        {
            var timeline = await _subscriptions.GetRevenueTimelineAsync(12);
            var sb = new StringBuilder();
            sb.AppendLine("Month,Revenue,Growth %");
            foreach (var r in timeline)
            {
                sb.AppendLine($"\"{r.Month}\",{r.Revenue},{r.Growth}");
            }

            await _audit.LogAsync("Export", "Reports", null, "Revenue", null, "Exported revenue CSV");
            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"RevenueTrends_{DateTime.Now:yyyyMMdd}.csv");
        }

    }
}
