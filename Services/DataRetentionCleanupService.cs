using JAS_MINE_IT15.Data;
using JAS_MINE_IT15.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace JAS_MINE_IT15.Services
{
    public class DataRetentionCleanupService : BackgroundService
    {
        private static readonly TimeSpan CheckInterval = TimeSpan.FromDays(1);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DataRetentionCleanupService> _logger;
        private readonly IOptions<RetentionSettings> _retentionOptions;
        private readonly IHostEnvironment _hostEnvironment;

        public DataRetentionCleanupService(
            IServiceScopeFactory scopeFactory,
            ILogger<DataRetentionCleanupService> logger,
            IOptions<RetentionSettings> retentionOptions,
            IHostEnvironment hostEnvironment)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _retentionOptions = retentionOptions;
            _hostEnvironment = hostEnvironment;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DataRetentionCleanupService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunCleanupCycle(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in data retention cleanup cycle.");
                }

                await Task.Delay(CheckInterval, stoppingToken);
            }
        }

        private async Task RunCleanupCycle(CancellationToken ct)
        {
            var retention = _retentionOptions.Value;
            var auditRetentionDays = Math.Max(1, retention.AuditLogRetentionDays);
            var tempFileRetentionDays = Math.Max(1, retention.TempFileRetentionDays);
            var resetRequestRetentionDays = Math.Max(1, retention.PasswordResetRequestRetentionDays);

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var auditCutoff = DateTime.UtcNow.AddDays(-auditRetentionDays);
            var oldInactiveLogs = await db.AuditLogs
                .Where(a => !a.IsActive && a.CreatedAt < auditCutoff)
                .ToListAsync(ct);

            if (oldInactiveLogs.Count > 0)
            {
                db.AuditLogs.RemoveRange(oldInactiveLogs);
                await db.SaveChangesAsync(ct);
                _logger.LogInformation("Deleted {Count} inactive audit logs older than {Days} days.", oldInactiveLogs.Count, auditRetentionDays);
            }

            var terminalStatuses = new[] { "completed", "rejected", "expired" };
            var resetCutoff = DateTime.UtcNow.AddDays(-resetRequestRetentionDays);
            var oldResetRequests = await db.PasswordResetRequests
                .Where(r => r.CreatedAt < resetCutoff && (!r.IsActive || terminalStatuses.Contains((r.Status ?? string.Empty).ToLower())))
                .ToListAsync(ct);

            if (oldResetRequests.Count > 0)
            {
                db.PasswordResetRequests.RemoveRange(oldResetRequests);
                await db.SaveChangesAsync(ct);
                _logger.LogInformation("Deleted {Count} password reset requests older than {Days} days.", oldResetRequests.Count, resetRequestRetentionDays);
            }

            var tempCutoff = DateTime.UtcNow.AddDays(-tempFileRetentionDays);
            var uploadsRoot = Path.Combine(_hostEnvironment.ContentRootPath, "wwwroot", "uploads");
            var tempDirs = new[]
            {
                Path.Combine(uploadsRoot, "temp"),
                Path.Combine(uploadsRoot, "tmp")
            };

            var deletedTempFiles = 0;
            foreach (var dir in tempDirs)
            {
                deletedTempFiles += DeleteOldFilesInDirectory(dir, tempCutoff);
            }

            if (deletedTempFiles > 0)
            {
                _logger.LogInformation("Deleted {Count} expired temporary file(s) older than {Days} days.", deletedTempFiles, tempFileRetentionDays);
            }
        }

        private int DeleteOldFilesInDirectory(string directoryPath, DateTime cutoffUtc)
        {
            if (!Directory.Exists(directoryPath))
            {
                return 0;
            }

            var deletedCount = 0;
            try
            {
                var files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    try
                    {
                        var info = new FileInfo(file);
                        if (info.LastWriteTimeUtc < cutoffUtc)
                        {
                            info.Delete();
                            deletedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed deleting temp file {File}.", file);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed scanning temp directory {Directory}.", directoryPath);
            }

            return deletedCount;
        }
    }
}
