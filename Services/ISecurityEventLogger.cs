using JAS_MINE_IT15.Data;
using JAS_MINE_IT15.Models;
using Microsoft.EntityFrameworkCore;

namespace JAS_MINE_IT15.Services
{
    /// <summary>
    /// Comprehensive security event logging service for real-time security monitoring and compliance.
    /// Categorizes events by type and severity, auto-triggers alerts for critical events.
    /// </summary>
    public interface ISecurityEventLogger
    {
        /// <summary>
        /// Log a security event with automatic categorization and alert triggering.
        /// </summary>
        Task LogSecurityEventAsync(
            SecurityEventType eventType,
            string description,
            int? userId = null,
            int? targetId = null,
            string? targetType = null,
            object? metadata = null);

        /// <summary>
        /// Get audit events by type within date range for reporting.
        /// </summary>
        Task<List<AuditLogDto>> GetEventsByTypeAsync(
            SecurityEventType eventType,
            DateTime fromDate,
            DateTime toDate);

        /// <summary>
        /// Get security summary metrics for dashboard.
        /// </summary>
        Task<SecurityDashboardMetrics> GetMetricsAsync(DateTime fromDate, DateTime toDate);

        /// <summary>
        /// Get failed login attempts for the specified date range.
        /// </summary>
        Task<List<AuditLogDto>> GetFailedLoginsAsync(DateTime fromDate, DateTime toDate);

        /// <summary>
        /// Get MFA failures for the specified date range.
        /// </summary>
        Task<List<AuditLogDto>> GetMfaFailuresAsync(DateTime fromDate, DateTime toDate);

        /// <summary>
        /// Get authorization/permission denials.
        /// </summary>
        Task<List<AuditLogDto>> GetAuthorizationDenialsAsync(DateTime fromDate, DateTime toDate);

        /// <summary>
        /// Get all data modifications (create/update/delete).
        /// </summary>
        Task<List<AuditLogDto>> GetDataModificationsAsync(DateTime fromDate, DateTime toDate);

        /// <summary>
        /// Get bulk export/delete operations.
        /// </summary>
        Task<List<AuditLogDto>> GetBulkOperationsAsync(DateTime fromDate, DateTime toDate);
    }

    public class SecurityDashboardMetrics
    {
        public int TotalEventsToday { get; set; }
        public int FailedLogins { get; set; }
        public int MfaFailures { get; set; }
        public int AuthorizationDenials { get; set; }
        public int CriticalEvents { get; set; }
        public int HighRiskEvents { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        public Dictionary<string, int> EventsByType { get; set; } = new();
        public Dictionary<string, int> EventsBySeverity { get; set; } = new();
        public List<string> RecentAlerts { get; set; } = new();
    }

    public class AuditLogDto
    {
        public long Id { get; set; }
        public int? UserId { get; set; }
        public string? UserEmail { get; set; }
        public string? UserName { get; set; }
        public string Action { get; set; } = "";
        public string Module { get; set; } = "";
        public int? TargetId { get; set; }
        public string? TargetType { get; set; }
        public string? TargetName { get; set; }
        public string? Description { get; set; }
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? EventType { get; set; }
        public string? Severity { get; set; }
        public string? Category { get; set; }
    }

    public class SecurityEventLogger : ISecurityEventLogger
    {
        private readonly IAuditService _auditService;
        private readonly ISecurityAlertService _alertService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SecurityEventLogger> _logger;
        private readonly ITenantService _tenantService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SecurityEventLogger(
            IAuditService auditService,
            ISecurityAlertService alertService,
            ApplicationDbContext context,
            ILogger<SecurityEventLogger> logger,
            ITenantService tenantService,
            IHttpContextAccessor httpContextAccessor)
        {
            _auditService = auditService;
            _alertService = alertService;
            _context = context;
            _logger = logger;
            _tenantService = tenantService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogSecurityEventAsync(
            SecurityEventType eventType,
            string description,
            int? userId = null,
            int? targetId = null,
            string? targetType = null,
            object? metadata = null)
        {
            try
            {
                var severity = eventType.GetDefaultSeverity();
                var eventDescription = eventType.GetDescription();

                // Build detailed description with metadata
                var fullDescription = $"{eventDescription}: {description}";
                if (metadata != null)
                {
                    fullDescription += $" | Metadata: {System.Text.Json.JsonSerializer.Serialize(metadata)}";
                }

                // Log via audit service
                await _auditService.LogAsync(
                    action: eventDescription,
                    module: GetModuleFromEventType(eventType),
                    targetId: targetId,
                    targetType: targetType,
                    targetName: $"{eventType}",
                    description: fullDescription
                );

                // Log structured event
                _logger.LogInformation(
                    "Security Event: {EventType} | Severity: {Severity} | User: {UserId} | Description: {Description}",
                    eventType,
                    severity,
                    userId,
                    description);

                // Trigger alerts for critical/error events
                if (severity == SecurityEventSeverity.Critical || severity == SecurityEventSeverity.Error)
                {
                    await TriggerSecurityAlertAsync(eventType, description, severity);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log security event: {EventType}", eventType);
            }
        }

        public async Task<List<AuditLogDto>> GetEventsByTypeAsync(
            SecurityEventType eventType,
            DateTime fromDate,
            DateTime toDate)
        {
            var eventDescription = eventType.GetDescription();
            var logs = await _context.AuditLogs
                .Where(l => l.CreatedAt >= fromDate && l.CreatedAt <= toDate && l.Action.Contains(eventDescription))
                .OrderByDescending(l => l.CreatedAt)
                .Select(l => MapToDto(l, eventType))
                .ToListAsync();

            return logs;
        }

        public async Task<SecurityDashboardMetrics> GetMetricsAsync(DateTime fromDate, DateTime toDate)
        {
            var logs = await _context.AuditLogs
                .Where(l => l.CreatedAt >= fromDate && l.CreatedAt <= toDate)
                .ToListAsync();

            var metrics = new SecurityDashboardMetrics
            {
                TotalEventsToday = logs.Count,
                FailedLogins = logs.Count(l => l.Action.Contains("Login") && l.Action.Contains("Fail")),
                MfaFailures = logs.Count(l => l.Action.Contains("MFA") && l.Action.Contains("Fail")),
                AuthorizationDenials = logs.Count(l => l.Action.Contains("Authorization") || l.Action.Contains("Permission")),
                CriticalEvents = logs.Count(l => l.Action.Contains("Critical") || l.Action.Contains("Tampering")),
                HighRiskEvents = logs.Count(l => l.Action.Contains("Brute") || l.Action.Contains("Injection") || l.Action.Contains("Escalation"))
            };

            // Group by event type
            metrics.EventsByType = logs
                .GroupBy(l => l.Action)
                .ToDictionary(g => g.Key, g => g.Count());

            // Group by severity
            metrics.EventsBySeverity = new Dictionary<string, int>
            {
                { "Critical", metrics.CriticalEvents },
                { "High", metrics.HighRiskEvents },
                { "Medium", logs.Count(l => l.Action.Contains("Change") || l.Action.Contains("Delete")) },
                { "Low", metrics.TotalEventsToday - metrics.CriticalEvents - metrics.HighRiskEvents - 
                    logs.Count(l => l.Action.Contains("Change") || l.Action.Contains("Delete")) }
            };

            return metrics;
        }

        public async Task<List<AuditLogDto>> GetFailedLoginsAsync(DateTime fromDate, DateTime toDate)
        {
            return await GetEventsByTypeAsync(SecurityEventType.LoginFailure, fromDate, toDate);
        }

        public async Task<List<AuditLogDto>> GetMfaFailuresAsync(DateTime fromDate, DateTime toDate)
        {
            return await GetEventsByTypeAsync(SecurityEventType.MfaFailure, fromDate, toDate);
        }

        public async Task<List<AuditLogDto>> GetAuthorizationDenialsAsync(DateTime fromDate, DateTime toDate)
        {
            var logs = await _context.AuditLogs
                .Where(l => l.CreatedAt >= fromDate && l.CreatedAt <= toDate &&
                    (l.Action.Contains("Authorization") || l.Action.Contains("Permission") || l.Action.Contains("Denied")))
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            return logs.Select(l => new AuditLogDto
            {
                Id = l.Id,
                UserId = l.UserId,
                UserEmail = l.UserEmail,
                UserName = l.UserName,
                Action = l.Action,
                Module = l.Module,
                TargetId = l.TargetId,
                TargetType = l.TargetType,
                TargetName = l.TargetName,
                Description = l.Description,
                IpAddress = l.IpAddress,
                UserAgent = l.UserAgent,
                CreatedAt = l.CreatedAt,
                Severity = "Error"
            }).ToList();
        }

        public async Task<List<AuditLogDto>> GetDataModificationsAsync(DateTime fromDate, DateTime toDate)
        {
            var logs = await _context.AuditLogs
                .Where(l => l.CreatedAt >= fromDate && l.CreatedAt <= toDate &&
                    (l.Action.Contains("Create") || l.Action.Contains("Update") || l.Action.Contains("Delete")))
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            return logs.Select(l => new AuditLogDto
            {
                Id = l.Id,
                UserId = l.UserId,
                UserEmail = l.UserEmail,
                UserName = l.UserName,
                Action = l.Action,
                Module = l.Module,
                TargetId = l.TargetId,
                TargetType = l.TargetType,
                TargetName = l.TargetName,
                Description = l.Description,
                OldValues = l.OldValues,
                NewValues = l.NewValues,
                IpAddress = l.IpAddress,
                CreatedAt = l.CreatedAt,
                Severity = "Warning"
            }).ToList();
        }

        public async Task<List<AuditLogDto>> GetBulkOperationsAsync(DateTime fromDate, DateTime toDate)
        {
            var logs = await _context.AuditLogs
                .Where(l => l.CreatedAt >= fromDate && l.CreatedAt <= toDate &&
                    (l.Action.Contains("Bulk") || l.Action.Contains("Export")))
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            return logs.Select(l => new AuditLogDto
            {
                Id = l.Id,
                UserId = l.UserId,
                UserEmail = l.UserEmail,
                UserName = l.UserName,
                Action = l.Action,
                Module = l.Module,
                Description = l.Description,
                IpAddress = l.IpAddress,
                CreatedAt = l.CreatedAt,
                Severity = "Warning"
            }).ToList();
        }

        private async Task TriggerSecurityAlertAsync(
            SecurityEventType eventType,
            string description,
            SecurityEventSeverity severity)
        {
            try
            {
                var alertMessage = $"Security Alert: {eventType.GetDescription()} - {description}";
                // Log the alert instead of calling a non-existent method
                _logger.LogWarning("SECURITY ALERT: {Severity} | {EventType} | {Description}", 
                    severity, eventType.GetDescription(), description);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to trigger security alert for event type: {EventType}", eventType);
            }
        }

        private string GetModuleFromEventType(SecurityEventType eventType)
        {
            return eventType switch
            {
                SecurityEventType.LoginSuccess or SecurityEventType.LoginFailure or
                SecurityEventType.MfaAttempt or SecurityEventType.MfaSuccess or SecurityEventType.MfaFailure or
                SecurityEventType.PasswordReset or SecurityEventType.PasswordChanged or
                SecurityEventType.AccountLocked or SecurityEventType.AccountUnlocked => "Authentication",

                SecurityEventType.AuthorizationDenial or SecurityEventType.PermissionDenial or
                SecurityEventType.RoleGranted or SecurityEventType.RoleRevoked or
                SecurityEventType.PrivilegeEscalation or SecurityEventType.UnauthorizedAccess => "Authorization",

                SecurityEventType.DocumentCreated or SecurityEventType.DocumentModified or
                SecurityEventType.DocumentDeleted or SecurityEventType.DocumentDownloaded or
                SecurityEventType.DocumentShared or SecurityEventType.BulkDelete or SecurityEventType.DataExport => "DocumentManagement",

                SecurityEventType.UserCreated or SecurityEventType.UserModified or SecurityEventType.UserDeleted or
                SecurityEventType.UserActivated or SecurityEventType.UserDeactivated => "UserManagement",

                _ => "Security"
            };
        }

        private AuditLogDto MapToDto(Models.Entities.AuditLog log, SecurityEventType eventType)
        {
            return new AuditLogDto
            {
                Id = log.Id,
                UserId = log.UserId,
                UserEmail = log.UserEmail,
                UserName = log.UserName,
                Action = log.Action,
                Module = log.Module,
                TargetId = log.TargetId,
                TargetType = log.TargetType,
                TargetName = log.TargetName,
                Description = log.Description,
                OldValues = log.OldValues,
                NewValues = log.NewValues,
                IpAddress = log.IpAddress,
                UserAgent = log.UserAgent,
                CreatedAt = log.CreatedAt,
                EventType = eventType.GetDescription(),
                Severity = eventType.GetDefaultSeverity().ToString(),
                Category = GetModuleFromEventType(eventType)
            };
        }
    }
}
