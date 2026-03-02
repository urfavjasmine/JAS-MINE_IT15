using JAS_MINE_IT15.Models;
using JAS_MINE_IT15.Models.Entities;

namespace JAS_MINE_IT15.Services
{
    /// <summary>
    /// Service interface for subscription lifecycle operations.
    /// </summary>
    public interface ISubscriptionService
    {
        Task<PagedResult<BarangaySubscription>> GetSubscriptionsAsync(
            string? search = null, string? status = null, int page = 1, int pageSize = 20);

        Task<BarangaySubscription?> GetActiveSubscriptionAsync(int barangayId);
        Task<List<SubscriptionPlan>> GetActivePlansAsync();
        Task<int> GetActiveSubscriptionCountAsync();
        Task<decimal> GetMonthlyRevenueAsync(DateTime month);
        Task<decimal> GetTotalRevenueAsync();

        /// <summary>
        /// Returns MoM revenue data for the last N months.
        /// </summary>
        Task<List<MonthlyRevenuePoint>> GetRevenueTimelineAsync(int months = 12);

        /// <summary>
        /// Returns distribution of active subscriptions by plan.
        /// </summary>
        Task<Dictionary<string, int>> GetPlanDistributionAsync();

        /// <summary>
        /// Returns churn data: subscriptions that expired or cancelled per month.
        /// </summary>
        Task<List<ChurnDataPoint>> GetChurnDataAsync(int months = 12);
    }

    public class MonthlyRevenuePoint
    {
        public string Month { get; set; } = "";
        public decimal Revenue { get; set; }
        public decimal Growth { get; set; } // percentage MoM
    }

    public class ChurnDataPoint
    {
        public string Month { get; set; } = "";
        public int Churned { get; set; }
        public int Active { get; set; }
        public decimal ChurnRate { get; set; } // percentage
    }
}
