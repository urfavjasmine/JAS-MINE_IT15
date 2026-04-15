using JAS_MINE_IT15.Data;
using Microsoft.EntityFrameworkCore;

namespace JAS_MINE_IT15.Services
{
    /// <summary>
    /// Background service that automatically expires subscriptions past their EndDate
    /// and marks overdue invoices. Runs every hour instead of only at startup.
    /// </summary>
    public class SubscriptionExpiryService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SubscriptionExpiryService> _logger;
        private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);

        public SubscriptionExpiryService(IServiceScopeFactory scopeFactory, ILogger<SubscriptionExpiryService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SubscriptionExpiryService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessExpirations(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in SubscriptionExpiryService cycle.");
                }

                await Task.Delay(CheckInterval, stoppingToken);
            }
        }

        private async Task ProcessExpirations(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Auto-expire subscriptions past EndDate
            var expiredSubs = await db.BarangaySubscriptions
                .Where(s => s.IsActive && s.Status == "Active" && s.EndDate < DateTime.Today)
                .ToListAsync(ct);
            foreach (var sub in expiredSubs)
            {
                sub.Status = "Expired";
                sub.UpdatedAt = DateTime.Now;
            }

            var expiredCount = expiredSubs.Count;
            if (expiredCount > 0)
            {
                await db.SaveChangesAsync(ct);
                _logger.LogInformation("Auto-expired {Count} subscription(s) past EndDate.", expiredCount);
            }

            // Auto-mark overdue invoices
            var overdueInvoices = await db.Invoices
                .Where(i => i.IsActive && i.Status == "Unpaid" && i.DueDate.HasValue && i.DueDate.Value < DateTime.Today)
                .ToListAsync(ct);
            foreach (var invoice in overdueInvoices)
            {
                invoice.Status = "Overdue";
                invoice.UpdatedAt = DateTime.Now;
            }

            var overdueCount = overdueInvoices.Count;
            if (overdueCount > 0)
            {
                await db.SaveChangesAsync(ct);
                _logger.LogInformation("Marked {Count} invoice(s) as Overdue.", overdueCount);
            }
        }
    }
}
