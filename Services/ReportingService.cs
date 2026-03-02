using JAS_MINE_IT15.Data;
using JAS_MINE_IT15.Models;
using JAS_MINE_IT15.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace JAS_MINE_IT15.Services
{
    public class ReportingService : IReportingService
    {
        private readonly ApplicationDbContext _db;
        private readonly ITenantService _tenant;

        public ReportingService(ApplicationDbContext db, ITenantService tenant)
        {
            _db = db;
            _tenant = tenant;
        }

        // ── Per-barangay summary (super_admin overview) ──
        public async Task<List<BarangayReportSummary>> GetBarangaySummariesAsync()
        {
            var barangays = await _db.Barangays
                .Where(b => b.IsActive)
                .OrderBy(b => b.Name)
                .ToListAsync();

            var result = new List<BarangayReportSummary>();

            foreach (var b in barangays)
            {
                var summary = await BuildBarangaySummary(b.Id, b.Name);
                result.Add(summary);
            }

            return result;
        }

        public async Task<BarangayReportSummary?> GetBarangayDetailAsync(int barangayId)
        {
            var brgy = await _db.Barangays.FirstOrDefaultAsync(b => b.Id == barangayId && b.IsActive);
            if (brgy == null) return null;
            return await BuildBarangaySummary(brgy.Id, brgy.Name);
        }

        private async Task<BarangayReportSummary> BuildBarangaySummary(int id, string name)
        {
            var sub = await _db.BarangaySubscriptions
                .Include(s => s.Plan)
                .Where(s => s.BarangayId == id && s.IsActive)
                .OrderByDescending(s => s.EndDate)
                .FirstOrDefaultAsync();

            return new BarangayReportSummary
            {
                BarangayId = id,
                BarangayName = name,
                TotalUsers = await _db.BusinessUsers.CountAsync(u => u.IsActive && u.BarangayId == id),
                TotalDocuments = await _db.KnowledgeDocuments.CountAsync(d => d.IsActive && !d.IsArchived && d.BarangayId == id),
                TotalPolicies = await _db.Policies.CountAsync(p => p.IsActive && !p.IsArchived && p.BarangayId == id),
                TotalLessons = await _db.LessonsLearned.CountAsync(l => l.IsActive && !l.IsArchived && l.BarangayId == id),
                TotalBestPractices = await _db.BestPractices.CountAsync(bp => bp.IsActive && !bp.IsArchived && bp.BarangayId == id),
                TotalDiscussions = await _db.KnowledgeDiscussions.CountAsync(d => d.IsActive && !d.IsArchived && d.BarangayId == id),
                TotalAnnouncements = await _db.Announcements.CountAsync(a => a.IsActive && !a.IsArchived && a.BarangayId == id),
                SubscriptionStatus = sub?.Status ?? "None",
                PlanName = sub?.Plan?.Name ?? "—",
                SubscriptionExpiry = sub?.EndDate
            };
        }

        // ── User activity ──
        public async Task<PagedResult<UserActivityRow>> GetUserActivityAsync(
            DateTime? from = null, DateTime? to = null,
            string? search = null, int page = 1, int pageSize = 20)
        {
            var usersQuery = _db.BusinessUsers.Where(u => u.IsActive);

            // Tenant filter
            if (!_tenant.IsSuperAdmin())
            {
                var brgyId = _tenant.GetCurrentBarangayId();
                usersQuery = usersQuery.Where(u => u.BarangayId == brgyId);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.ToLower();
                usersQuery = usersQuery.Where(u =>
                    u.FullName.ToLower().Contains(term) ||
                    u.Email.ToLower().Contains(term));
            }

            var totalCount = await usersQuery.CountAsync();
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var users = await usersQuery
                .OrderBy(u => u.FullName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var rows = new List<UserActivityRow>();
            foreach (var u in users)
            {
                var loginQuery = _db.AuditLogs
                    .Where(a => a.UserId == u.Id && a.Action == "Login" && a.IsActive);

                if (from.HasValue)
                    loginQuery = loginQuery.Where(a => a.CreatedAt >= from.Value);
                if (to.HasValue)
                    loginQuery = loginQuery.Where(a => a.CreatedAt <= to.Value.Date.AddDays(1));

                var docsCreated = await CountCreated(_db.KnowledgeDocuments, d => d.UploadedById == u.Id, d => d.CreatedAt, from, to);
                var policiesCreated = await CountCreated(_db.Policies, p => p.AuthorId == u.Id, p => p.CreatedAt, from, to);
                var lessonsCreated = await CountCreated(_db.LessonsLearned, l => l.SubmittedById == u.Id, l => l.CreatedAt, from, to);
                var discussionsCreated = await CountCreated(_db.KnowledgeDiscussions, d => d.AuthorId == u.Id, d => d.CreatedAt, from, to);

                rows.Add(new UserActivityRow
                {
                    UserId = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    Role = u.Role,
                    BarangayName = u.BarangayName,
                    LastLoginAt = u.LastLoginAt,
                    LoginCount = await loginQuery.CountAsync(),
                    DocumentsCreated = docsCreated,
                    PoliciesCreated = policiesCreated,
                    LessonsCreated = lessonsCreated,
                    DiscussionsCreated = discussionsCreated,
                    TotalContributions = docsCreated + policiesCreated + lessonsCreated + discussionsCreated
                });
            }

            return new PagedResult<UserActivityRow>
            {
                Items = rows,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        private async Task<int> CountCreated<T>(
            DbSet<T> set,
            System.Linq.Expressions.Expression<Func<T, bool>> authorFilter,
            System.Linq.Expressions.Expression<Func<T, DateTime>> dateSelector,
            DateTime? from, DateTime? to) where T : class
        {
            var q = set.Where(authorFilter);
            if (from.HasValue || to.HasValue)
            {
                // Build date predicate dynamically
                var param = dateSelector.Parameters[0];
                var dateExpr = dateSelector.Body;

                if (from.HasValue)
                {
                    var ge = System.Linq.Expressions.Expression.GreaterThanOrEqual(
                        dateExpr, System.Linq.Expressions.Expression.Constant(from.Value));
                    q = q.Where(System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(ge, param));
                }
                if (to.HasValue)
                {
                    var le = System.Linq.Expressions.Expression.LessThanOrEqual(
                        dateExpr, System.Linq.Expressions.Expression.Constant(to.Value.Date.AddDays(1)));
                    q = q.Where(System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(le, param));
                }
            }
            return await q.CountAsync();
        }

        // ── Content lifecycle (status distribution) ──
        public async Task<List<ContentLifecycleRow>> GetContentLifecycleAsync(DateTime? from = null, DateTime? to = null)
        {
            var rows = new List<ContentLifecycleRow>();

            rows.Add(await BuildLifecycle("Documents", _db.KnowledgeDocuments
                .Where(d => d.IsActive && !d.IsArchived)
                .FilterByTenant(_tenant, d => d.BarangayId),
                d => d.Status, d => d.CreatedAt, from, to));

            rows.Add(await BuildLifecycle("Policies", _db.Policies
                .Where(p => p.IsActive && !p.IsArchived)
                .FilterByTenant(_tenant, p => p.BarangayId),
                p => p.Status, p => p.CreatedAt, from, to));

            rows.Add(await BuildLifecycle("Lessons Learned", _db.LessonsLearned
                .Where(l => l.IsActive && !l.IsArchived)
                .FilterByTenant(_tenant, l => l.BarangayId),
                l => l.Status, l => l.CreatedAt, from, to));

            rows.Add(await BuildLifecycle("Best Practices", _db.BestPractices
                .Where(b => b.IsActive && !b.IsArchived)
                .FilterByTenant(_tenant, b => b.BarangayId),
                b => b.Status, b => b.CreatedAt, from, to));

            return rows;
        }

        private async Task<ContentLifecycleRow> BuildLifecycle<T>(
            string module, IQueryable<T> baseQuery,
            System.Linq.Expressions.Expression<Func<T, string>> statusSelector,
            System.Linq.Expressions.Expression<Func<T, DateTime>> dateSelector,
            DateTime? from, DateTime? to) where T : class
        {
            var q = baseQuery;

            // Date filter
            if (from.HasValue || to.HasValue)
            {
                var param = dateSelector.Parameters[0];
                var dateExpr = dateSelector.Body;

                if (from.HasValue)
                {
                    var ge = System.Linq.Expressions.Expression.GreaterThanOrEqual(
                        dateExpr, System.Linq.Expressions.Expression.Constant(from.Value));
                    q = q.Where(System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(ge, param));
                }
                if (to.HasValue)
                {
                    var le = System.Linq.Expressions.Expression.LessThanOrEqual(
                        dateExpr, System.Linq.Expressions.Expression.Constant(to.Value.Date.AddDays(1)));
                    q = q.Where(System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(le, param));
                }
            }

            // Group by status
            var compiled = statusSelector.Compile();
            var items = await q.ToListAsync();
            var groups = items.GroupBy(compiled).ToDictionary(g => (g.Key ?? "").ToLower(), g => g.Count());

            return new ContentLifecycleRow
            {
                Module = module,
                Draft = groups.GetValueOrDefault("draft"),
                Pending = groups.GetValueOrDefault("pending"),
                Approved = groups.GetValueOrDefault("approved"),
                Rejected = groups.GetValueOrDefault("rejected"),
                Archived = groups.GetValueOrDefault("archived"),
                Total = items.Count
            };
        }

        // ── Content timeline (monthly creation counts) ──
        public async Task<List<TimeSeriesPoint>> GetContentTimelineAsync(string module, int months = 12)
        {
            var result = new List<TimeSeriesPoint>();
            var now = DateTime.Today;

            for (int i = months - 1; i >= 0; i--)
            {
                var monthStart = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
                var monthEnd = monthStart.AddMonths(1);
                var label = monthStart.ToString("MMM yyyy");

                int count = module.ToLower() switch
                {
                    "documents" => await _db.KnowledgeDocuments
                        .Where(d => d.IsActive && !d.IsArchived && d.CreatedAt >= monthStart && d.CreatedAt < monthEnd)
                        .FilterByTenant(_tenant, d => d.BarangayId)
                        .CountAsync(),
                    "policies" => await _db.Policies
                        .Where(p => p.IsActive && !p.IsArchived && p.CreatedAt >= monthStart && p.CreatedAt < monthEnd)
                        .FilterByTenant(_tenant, p => p.BarangayId)
                        .CountAsync(),
                    "lessons" => await _db.LessonsLearned
                        .Where(l => l.IsActive && !l.IsArchived && l.CreatedAt >= monthStart && l.CreatedAt < monthEnd)
                        .FilterByTenant(_tenant, l => l.BarangayId)
                        .CountAsync(),
                    "discussions" => await _db.KnowledgeDiscussions
                        .Where(d => d.IsActive && !d.IsArchived && d.CreatedAt >= monthStart && d.CreatedAt < monthEnd)
                        .FilterByTenant(_tenant, d => d.BarangayId)
                        .CountAsync(),
                    _ => 0
                };

                result.Add(new TimeSeriesPoint { Label = label, Count = count });
            }

            return result;
        }

        // ── Dashboard counts ──
        public async Task<ReportDashboardCounts> GetDashboardCountsAsync()
        {
            return new ReportDashboardCounts
            {
                TotalBarangays = await _db.Barangays.CountAsync(b => b.IsActive),
                TotalUsers = await _db.BusinessUsers.CountAsync(u => u.IsActive),
                TotalDocuments = await _db.KnowledgeDocuments
                    .Where(d => d.IsActive && !d.IsArchived)
                    .FilterByTenant(_tenant, d => d.BarangayId)
                    .CountAsync(),
                TotalPolicies = await _db.Policies
                    .Where(p => p.IsActive && !p.IsArchived)
                    .FilterByTenant(_tenant, p => p.BarangayId)
                    .CountAsync(),
                ActiveSubscriptions = await _db.BarangaySubscriptions
                    .CountAsync(s => s.IsActive && s.Status == "Active"),
                TotalRevenue = await _db.SubscriptionPayments
                    .Where(p => p.IsActive && (p.Status == "Approved" || p.Status == "Paid"))
                    .SumAsync(p => (decimal?)p.Amount) ?? 0
            };
        }
    }
}
