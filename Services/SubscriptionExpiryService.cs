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
            var expiredCount = await db.Database.ExecuteSqlRawAsync(@"
                UPDATE dbo.BarangaySubscriptions
                SET Status = 'Expired', UpdatedAt = GETDATE()
                WHERE IsActive = 1
                  AND Status = 'Active'
                  AND EndDate < CAST(GETDATE() AS DATE)
            ", ct);
            if (expiredCount > 0)
                _logger.LogInformation("Auto-expired {Count} subscription(s) past EndDate.", expiredCount);

            // Auto-mark overdue invoices
            var overdueCount = await db.Database.ExecuteSqlRawAsync(@"
                UPDATE dbo.Invoices
                SET Status = 'Overdue', UpdatedAt = GETDATE()
                WHERE IsActive = 1
                  AND Status = 'Unpaid'
                  AND DueDate < CAST(GETDATE() AS DATE)
            ", ct);
            if (overdueCount > 0)
                _logger.LogInformation("Marked {Count} invoice(s) as Overdue.", overdueCount);
        }
    }
}
