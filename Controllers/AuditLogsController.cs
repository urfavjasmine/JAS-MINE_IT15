using JAS_MINE_IT15.Data;
using JAS_MINE_IT15.Models;
using JAS_MINE_IT15.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace JAS_MINE_IT15.Controllers
{
    /// <summary>
    /// Comprehensive audit log management and security event monitoring.
    /// Displays all audit logs with filtering, export, and real-time alerts.
    /// Requires super_admin or barangay_admin roles.
    /// </summary>
    [Authorize(Roles = "super_admin,barangay_admin")]
    [Route("[controller]")]
    public class AuditLogsController : BaseAppController
    {
        private readonly ISecurityEventLogger _securityEventLogger;
        private readonly ILogger<AuditLogsController> _logger;

        public AuditLogsController(
            ISecurityEventLogger securityEventLogger,
            ILogger<AuditLogsController> logger,
            ApplicationDbContext context) : base(context)
        {
            _securityEventLogger = securityEventLogger;
            _logger = logger;
        }

        /// <summary>
        /// Display comprehensive audit logs with filtering and search.
        /// </summary>
        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index(
            string? search = null,
            string? module = null,
            string? action = null,
            string? eventType = null,
            string? severity = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int page = 1,
            int pageSize = 50)
        {
            try
            {
                // Set defaults - show all logs if no date range specified
                startDate ??= new DateTime(2000, 1, 1);
                endDate ??= DateTime.Now.AddDays(1).Date;  // Include all of today

                // Query base logs
                var query = _context.AuditLogs
                    .AsNoTracking()
                    .Where(l => l.CreatedAt >= startDate && l.CreatedAt < endDate);

                // Apply filters
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var searchLower = search.ToLower();
                    query = query.Where(l =>
                        l.Action.ToLower().Contains(searchLower) ||
                        l.Module.ToLower().Contains(searchLower) ||
                        l.Description!.ToLower().Contains(searchLower) ||
                        l.UserEmail!.ToLower().Contains(searchLower) ||
                        l.UserName!.ToLower().Contains(searchLower));
                }

                if (!string.IsNullOrWhiteSpace(module))
                {
                    query = query.Where(l => l.Module == module);
                }

                if (!string.IsNullOrWhiteSpace(action))
                {
                    query = query.Where(l => l.Action.Contains(action));
                }

                // Get total count for pagination
                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                // Get paginated results
                var logs = await query
                    .OrderByDescending(l => l.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Build view model
                var viewModel = new AuditLogDisplayModel
                {
                    Logs = logs,
                    Search = search ?? "",
                    Module = module ?? "",
                    Action = action ?? "",
                    EventType = eventType ?? "",
                    Severity = severity ?? "",
                    StartDate = startDate.Value,
                    EndDate = endDate.Value,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = totalPages,
                    AvailableModules = await GetAvailableModulesAsync(),
                    AvailableActions = await GetAvailableActionsAsync(),
                    AvailableEventTypes = await GetAvailableEventTypesAsync()
                };

                await LogAuditAsync("View", "AuditLogs", description: "Viewed audit logs");
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error viewing audit logs");
                TempData["Error"] = "Error loading audit logs";
                return View(new AuditLogDisplayModel());
            }
        }

        /// <summary>
        /// Display security dashboard with metrics and real-time alerts.
        /// </summary>
        [HttpGet("Dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var fromDate = DateTime.Now.AddHours(-24);
                var toDate = DateTime.Now;

                var metrics = await _securityEventLogger.GetMetricsAsync(fromDate, toDate);
                var failedLogins = await _securityEventLogger.GetFailedLoginsAsync(fromDate, toDate);
                var mfaFailures = await _securityEventLogger.GetMfaFailuresAsync(fromDate, toDate);
                var authDenials = await _securityEventLogger.GetAuthorizationDenialsAsync(fromDate, toDate);

                var dashboard = new SecurityDashboardModel
                {
                    Metrics = metrics,
                    RecentFailedLogins = failedLogins.Take(10).ToList(),
                    RecentMfaFailures = mfaFailures.Take(10).ToList(),
                    RecentAuthDenials = authDenials.Take(10).ToList(),
                    CriticalAlertsCount = metrics.CriticalEvents,
                    HighRiskAlertsCount = metrics.HighRiskEvents
                };

                await LogAuditAsync("View", "SecurityDashboard", description: "Viewed security dashboard");
                return View(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading security dashboard");
                TempData["Error"] = "Error loading dashboard";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Display failed login attempts for the specified date range.
        /// </summary>
        [HttpGet("FailedLogins")]
        public async Task<IActionResult> FailedLogins(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                startDate ??= DateTime.Now.AddDays(-30);
                endDate ??= DateTime.Now;

                var logs = await _securityEventLogger.GetFailedLoginsAsync(startDate.Value, endDate.Value);

                var viewModel = new SecurityEventReportModel
                {
                    EventType = "Failed Login Attempts",
                    Events = logs,
                    StartDate = startDate.Value,
                    EndDate = endDate.Value,
                    TotalCount = logs.Count,
                    CriticalCount = logs.Count(l => l.Severity == "Critical"),
                    WarningCount = logs.Count(l => l.Severity == "Warning"),
                    GeneratedAt = DateTime.UtcNow
                };

                await LogAuditAsync("View", "FailedLogins", description: "Viewed failed login report");
                return View("SecurityEventReport", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading failed logins");
                TempData["Error"] = "Error loading failed login report";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Display MFA failures for the specified date range.
        /// </summary>
        [HttpGet("MfaFailures")]
        public async Task<IActionResult> MfaFailures(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                startDate ??= DateTime.Now.AddDays(-7);
                endDate ??= DateTime.Now;

                var logs = await _securityEventLogger.GetMfaFailuresAsync(startDate.Value, endDate.Value);

                var viewModel = new SecurityEventReportModel
                {
                    EventType = "MFA Verification Failures",
                    Events = logs,
                    StartDate = startDate.Value,
                    EndDate = endDate.Value,
                    TotalCount = logs.Count,
                    CriticalCount = logs.Count(l => l.Severity == "Critical"),
                    WarningCount = logs.Count(l => l.Severity == "Warning"),
                    GeneratedAt = DateTime.UtcNow
                };

                await LogAuditAsync("View", "MfaFailures", description: "Viewed MFA failure report");
                return View("SecurityEventReport", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading MFA failures");
                TempData["Error"] = "Error loading MFA failure report";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Display authorization/permission denials.
        /// </summary>
        [HttpGet("AuthorizationDenials")]
        public async Task<IActionResult> AuthorizationDenials(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                startDate ??= DateTime.Now.AddDays(-30);
                endDate ??= DateTime.Now;

                var logs = await _securityEventLogger.GetAuthorizationDenialsAsync(startDate.Value, endDate.Value);

                var viewModel = new SecurityEventReportModel
                {
                    EventType = "Authorization Denials",
                    Events = logs,
                    StartDate = startDate.Value,
                    EndDate = endDate.Value,
                    TotalCount = logs.Count,
                    CriticalCount = logs.Count(l => l.Severity == "Critical"),
                    WarningCount = logs.Count(l => l.Severity == "Warning"),
                    GeneratedAt = DateTime.UtcNow
                };

                await LogAuditAsync("View", "AuthDenials", description: "Viewed authorization denial report");
                return View("SecurityEventReport", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading authorization denials");
                TempData["Error"] = "Error loading authorization denial report";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Display data modifications (create, update, delete operations).
        /// </summary>
        [HttpGet("DataModifications")]
        public async Task<IActionResult> DataModifications(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                startDate ??= DateTime.Now.AddDays(-30);
                endDate ??= DateTime.Now;

                var logs = await _securityEventLogger.GetDataModificationsAsync(startDate.Value, endDate.Value);

                var viewModel = new SecurityEventReportModel
                {
                    EventType = "Data Modifications",
                    Events = logs,
                    StartDate = startDate.Value,
                    EndDate = endDate.Value,
                    TotalCount = logs.Count,
                    WarningCount = logs.Count(l => l.Severity == "Warning"),
                    GeneratedAt = DateTime.UtcNow
                };

                await LogAuditAsync("View", "DataMods", description: "Viewed data modification report");
                return View("SecurityEventReport", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading data modifications");
                TempData["Error"] = "Error loading data modification report";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Export audit logs to CSV file.
        /// </summary>
        [HttpGet("Export")]
        public async Task<IActionResult> ExportCsv(
            string? search = null,
            string? module = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            try
            {
                startDate ??= DateTime.Now.AddDays(-30);
                endDate ??= DateTime.Now;

                var query = _context.AuditLogs
                    .AsNoTracking()
                    .Where(l => l.CreatedAt >= startDate && l.CreatedAt <= endDate);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var searchLower = search.ToLower();
                    query = query.Where(l =>
                        l.Action.ToLower().Contains(searchLower) ||
                        l.Description!.ToLower().Contains(searchLower));
                }

                if (!string.IsNullOrWhiteSpace(module))
                {
                    query = query.Where(l => l.Module == module);
                }

                var logs = await query.OrderByDescending(l => l.CreatedAt).ToListAsync();

                // Build CSV
                var csv = new StringBuilder();
                csv.AppendLine("ID,User Email,User Name,Action,Module,Target ID,Target Type,Description,IP Address,Created At");

                foreach (var log in logs)
                {
                    csv.AppendLine($"\"{log.Id}\",\"{log.UserEmail}\",\"{log.UserName}\",\"{log.Action}\",\"{log.Module}\"," +
                        $"\"{log.TargetId}\",\"{log.TargetType}\",\"{log.Description?.Replace("\"", "\"\"")}\",\"{log.IpAddress}\",\"{log.CreatedAt:yyyy-MM-dd HH:mm:ss}\"");
                }

                await LogAuditAsync("Export", "AuditLogs", description: $"Exported {logs.Count} audit logs to CSV");

                return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"audit-logs-{DateTime.Now:yyyy-MM-dd}.csv");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting audit logs");
                TempData["Error"] = "Error exporting audit logs";
                return RedirectToAction(nameof(Index));
            }
        }



        /// <summary>
        /// View details of a specific audit log entry.
        /// </summary>
        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(long id)
        {
            try
            {
                var log = await _context.AuditLogs.FindAsync(id);
                if (log == null)
                {
                    return NotFound();
                }

                await LogAuditAsync("View", "AuditLog", targetId: (int?)id, description: $"Viewed audit log details");
                return View(log);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading audit log details");
                return NotFound();
            }
        }

        // Helper methods
        private async Task<List<SelectListItem>> GetAvailableModulesAsync()
        {
            var modules = await _context.AuditLogs
                .Select(l => l.Module)
                .Distinct()
                .OrderBy(m => m)
                .ToListAsync();

            return modules.Select(m => new SelectListItem(m, m)).ToList();
        }

        private async Task<List<SelectListItem>> GetAvailableActionsAsync()
        {
            var actions = await _context.AuditLogs
                .Select(l => l.Action)
                .Distinct()
                .OrderBy(a => a)
                .ToListAsync();

            return actions.Select(a => new SelectListItem(a, a)).ToList();
        }

        private async Task<List<SelectListItem>> GetAvailableEventTypesAsync()
        {
            var eventTypes = typeof(SecurityEventType)
                .GetFields()
                .Where(f => f.IsLiteral)
                .Select(f => new SelectListItem(f.Name, f.Name))
                .ToList();

            return await Task.FromResult(eventTypes);
        }
    }

    // View Models
    public class AuditLogDisplayModel
    {
        public List<Models.Entities.AuditLog> Logs { get; set; } = new();
        public string Search { get; set; } = "";
        public string Module { get; set; } = "";
        public string Action { get; set; } = "";
        public string EventType { get; set; } = "";
        public string Severity { get; set; } = "";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public List<SelectListItem> AvailableModules { get; set; } = new();
        public List<SelectListItem> AvailableActions { get; set; } = new();
        public List<SelectListItem> AvailableEventTypes { get; set; } = new();
    }

    public class SecurityDashboardModel
    {
        public SecurityDashboardMetrics Metrics { get; set; } = new();
        public List<AuditLogDto> RecentFailedLogins { get; set; } = new();
        public List<AuditLogDto> RecentMfaFailures { get; set; } = new();
        public List<AuditLogDto> RecentAuthDenials { get; set; } = new();
        public int CriticalAlertsCount { get; set; }
        public int HighRiskAlertsCount { get; set; }
    }

    public class SecurityEventReportModel
    {
        public string EventType { get; set; } = "";
        public List<AuditLogDto> Events { get; set; } = new();
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalCount { get; set; }
        public int CriticalCount { get; set; }
        public int WarningCount { get; set; }
        public DateTime GeneratedAt { get; set; }
    }
}
