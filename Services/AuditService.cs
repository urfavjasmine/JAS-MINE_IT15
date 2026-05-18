using JAS_MINE_IT15.Data;
using JAS_MINE_IT15.Models;
using JAS_MINE_IT15.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace JAS_MINE_IT15.Services
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantService _tenantService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuditService> _logger;
        private readonly IAuditLogHashService _auditLogHashService;
        private readonly ISecurityAlertService _securityAlertService;

        public AuditService(
            ApplicationDbContext context,
            ITenantService tenantService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AuditService> logger,
            IAuditLogHashService auditLogHashService,
            ISecurityAlertService securityAlertService)
        {
            _context = context;
            _tenantService = tenantService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _auditLogHashService = auditLogHashService;
            _securityAlertService = securityAlertService;
        }

        public async Task LogAsync(string action, string module, int? targetId, string? targetType,
            string? targetName, string description, int? barangayId = null)
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                var userIdStr = httpContext?.Session.GetString("UserId");
                int.TryParse(userIdStr, out var userId);

                // Mask sensitive information before storage
                var userEmail = _tenantService.GetCurrentUserEmail();
                var maskedEmail = DataMaskingHelper.MaskEmail(userEmail);
                var maskedDescription = DataMaskingHelper.MaskSensitiveInformation(description);
                var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString();
                var maskedIp = DataMaskingHelper.MaskIpAddress(ipAddress);

                var log = new AuditLog
                {
                    UserId = userId > 0 ? userId : null,
                    UserEmail = maskedEmail,
                    UserName = httpContext?.Session.GetString("UserName"),
                    Action = action,
                    Module = module,
                    TargetId = targetId,
                    TargetType = targetType,
                    TargetName = targetName,
                    Description = maskedDescription,
                    IpAddress = maskedIp,
                    UserAgent = httpContext?.Request.Headers["User-Agent"].FirstOrDefault(),
                    SessionId = httpContext?.Session.Id,
                    BarangayId = barangayId ?? _tenantService.GetCurrentBarangayId(),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.AuditLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write audit log: {Action} {Module}", action, module);
            }
        }

        public async Task<AuditLogIntegrityReport> VerifyIntegrityAsync(CancellationToken cancellationToken = default)
        {
            var logs = await _context.AuditLogs
                .AsNoTracking()
                .Where(l => l.Hash != null && l.Hash != "")
                .OrderBy(l => l.Id)
                .ToListAsync(cancellationToken);

            if (logs.Count == 0)
            {
                return new AuditLogIntegrityReport
                {
                    IsValid = true,
                    CheckedCount = 0,
                    Error = "No hashed audit logs found yet."
                };
            }

            string? expectedPreviousHash = null;
            for (var i = 0; i < logs.Count; i++)
            {
                var log = logs[i];
                var algorithm = string.IsNullOrWhiteSpace(log.HashAlgorithm)
                    ? AuditLogIntegrity.LegacySha256Algorithm
                    : log.HashAlgorithm;
                var expectedHash = _auditLogHashService.ComputeHash(log, expectedPreviousHash, algorithm);

                if (!string.Equals(log.PreviousHash, expectedPreviousHash, StringComparison.OrdinalIgnoreCase)
                    || !string.Equals(log.Hash, expectedHash, StringComparison.OrdinalIgnoreCase))
                {
                    await _securityAlertService.RecordAuditIntegrityFailureAsync(
                        log.Id,
                        "Audit log hash chain mismatch detected.",
                        cancellationToken);

                    return new AuditLogIntegrityReport
                    {
                        IsValid = false,
                        CheckedCount = i + 1,
                        FirstBrokenLogId = log.Id,
                        Error = "Audit log hash chain mismatch detected."
                    };
                }

                expectedPreviousHash = log.Hash;
            }

            return new AuditLogIntegrityReport
            {
                IsValid = true,
                CheckedCount = logs.Count
            };
        }
    }
}
