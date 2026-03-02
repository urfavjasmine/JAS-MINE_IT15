using JAS_MINE_IT15.Data;
using JAS_MINE_IT15.Models;
using JAS_MINE_IT15.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace JAS_MINE_IT15.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantService _tenantService;

        public SubscriptionService(ApplicationDbContext context, ITenantService tenantService)
        {
            _context = context;
            _tenantService = tenantService;
        }

        public async Task<PagedResult<BarangaySubscription>> GetSubscriptionsAsync(
            string? search = null, string? status = null, int page = 1, int pageSize = 20)
        {
            var query = _context.BarangaySubscriptions
                .Include(s => s.Barangay)
                .Include(s => s.Plan)
                .Where(s => s.IsActive);

            if (!string.IsNullOrWhiteSpace(status) && status != "all")
                query = query.Where(s => s.Status == status);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.ToLower();
                query = query.Where(s =>
                    (s.Barangay != null && s.Barangay.Name.ToLower().Contains(term)) ||
                    (s.Plan != null && s.Plan.Name.ToLower().Contains(term)));
            }

            return await query
                .OrderByDescending(s => s.CreatedAt)
                .ToPagedResultAsync(page, pageSize);
        }

        public async Task<BarangaySubscription?> GetActiveSubscriptionAsync(int barangayId)
        {
            return await _context.BarangaySubscriptions
                .Include(s => s.Plan)
                .Include(s => s.Barangay)
                .Where(s => s.BarangayId == barangayId && s.IsActive && s.Status == "Active")
                .OrderByDescending(s => s.EndDate)
                .FirstOrDefaultAsync();
        }

        public async Task<List<SubscriptionPlan>> GetActivePlansAsync()
        {
            return await _context.SubscriptionPlans
                .Where(p => p.IsActive)
                .OrderBy(p => p.Price)
                .ToListAsync();
        }

        public async Task<int> GetActiveSubscriptionCountAsync()
        {
            return await _context.BarangaySubscriptions
                .CountAsync(s => s.IsActive && s.Status == "Active");
        }

        public async Task<decimal> GetMonthlyRevenueAsync(DateTime month)
        {
            var start = new DateTime(month.Year, month.Month, 1);
            var end = start.AddMonths(1);
            return await _context.SubscriptionPayments
                .Where(p => p.IsActive && p.Status == "Approved" &&
                       p.PaymentDate >= start && p.PaymentDate < end)
                .SumAsync(p => p.Amount);
        }

        public async Task<decimal> GetTotalRevenueAsync()
        {
            return await _context.SubscriptionPayments
                .Where(p => p.IsActive && (p.Status == "Approved" || p.Status == "Paid"))
                .SumAsync(p => p.Amount);
        }

        public async Task<List<MonthlyRevenuePoint>> GetRevenueTimelineAsync(int months = 12)
        {
            var result = new List<MonthlyRevenuePoint>();
            var now = DateTime.Today;
            decimal previousRevenue = 0;

            for (int i = months - 1; i >= 0; i--)
            {
                var month = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
                var end = month.AddMonths(1);

                var revenue = await _context.SubscriptionPayments
                    .Where(p => p.IsActive && (p.Status == "Approved" || p.Status == "Paid") &&
                           p.PaymentDate >= month && p.PaymentDate < end)
                    .SumAsync(p => (decimal?)p.Amount) ?? 0;

                var growth = previousRevenue > 0
                    ? Math.Round((revenue - previousRevenue) / previousRevenue * 100, 1)
                    : 0;

                result.Add(new MonthlyRevenuePoint
                {
                    Month = month.ToString("MMM yyyy"),
                    Revenue = revenue,
                    Growth = growth
                });

                previousRevenue = revenue;
            }
            return result;
        }

        public async Task<Dictionary<string, int>> GetPlanDistributionAsync()
        {
            return await _context.BarangaySubscriptions
                .Where(s => s.IsActive && s.Status == "Active")
                .Include(s => s.Plan)
                .GroupBy(s => s.Plan != null ? s.Plan.Name : "Unknown")
                .Select(g => new { Plan = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Plan, g => g.Count);
        }

        public async Task<List<ChurnDataPoint>> GetChurnDataAsync(int months = 12)
        {
            var result = new List<ChurnDataPoint>();
            var now = DateTime.Today;

            for (int i = months - 1; i >= 0; i--)
            {
                var month = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
                var end = month.AddMonths(1);

                var churned = await _context.BarangaySubscriptions
                    .CountAsync(s => s.IsActive &&
                        (s.Status == "Expired" || s.Status == "Cancelled") &&
                        s.UpdatedAt.HasValue && s.UpdatedAt.Value >= month && s.UpdatedAt.Value < end);

                var active = await _context.BarangaySubscriptions
                    .CountAsync(s => s.IsActive && s.Status == "Active" && s.StartDate < end);

                var total = churned + active;
                var rate = total > 0 ? Math.Round((decimal)churned / total * 100, 1) : 0;

                result.Add(new ChurnDataPoint
                {
                    Month = month.ToString("MMM yyyy"),
                    Churned = churned,
                    Active = active,
                    ChurnRate = rate
                });
            }
            return result;
        }
    }
}
