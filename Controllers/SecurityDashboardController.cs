using JAS_MINE_IT15.Data;
using JAS_MINE_IT15.Models;
using JAS_MINE_IT15.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text;

namespace JAS_MINE_IT15.Controllers
{
    [Authorize(Roles = "super_admin,barangay_admin,admin")]
    public class SecurityDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ITenantService _tenantService;

        public SecurityDashboardController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            ITenantService tenantService)
        {
            _context = context;
            _userManager = userManager;
            _tenantService = tenantService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string range = "24h", int failedLoginThreshold = 20, int apiFailureThreshold = 20)
        {
            var vm = await BuildDashboardViewModelAsync(range, failedLoginThreshold, apiFailureThreshold);
            return View(vm);
        }

        [HttpGet("SecurityDashboard/export/csv")]
        public async Task<IActionResult> ExportCsv(string range = "24h", int failedLoginThreshold = 20, int apiFailureThreshold = 20)
        {
            var vm = await BuildDashboardViewModelAsync(range, failedLoginThreshold, apiFailureThreshold);
            var csv = BuildCsv(vm);
            var fileName = $"security-dashboard-{vm.Range}-{DateTime.Now:yyyyMMddHHmmss}.csv";
            return File(Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
        }

        [HttpGet("SecurityDashboard/export/pdf")]
        public async Task<IActionResult> ExportPdf(string range = "24h", int failedLoginThreshold = 20, int apiFailureThreshold = 20)
        {
            var vm = await BuildDashboardViewModelAsync(range, failedLoginThreshold, apiFailureThreshold);

            QuestPDF.Settings.License = LicenseType.Community;
            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(24);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("JAS-MINE Security Dashboard Report").Bold().FontSize(16);
                        col.Item().Text($"Scope: {(vm.IsSuperAdmin ? "Super Admin" : "Admin")} | Range: {vm.RangeLabel} | Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                    });

                    page.Content().Column(col =>
                    {
                        col.Spacing(10);

                        col.Item().Text("Security Metrics").Bold();
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1);
                            });

                            table.Cell().Text("Failed Login Attempts (Current Counters)").SemiBold();
                            table.Cell().Text(vm.FailedLoginAttemptsTotal.ToString());
                            table.Cell().Text($"Failed Login Signals ({vm.RangeLabel})").SemiBold();
                            table.Cell().Text(vm.FailedLoginEventsInRange.ToString());
                            table.Cell().Text("Locked Accounts").SemiBold();
                            table.Cell().Text(vm.LockedAccountsCount.ToString());
                            table.Cell().Text($"API Requests ({vm.RangeLabel})").SemiBold();
                            table.Cell().Text(vm.ApiRequestsInRange.ToString());
                            table.Cell().Text($"API Failed Requests ({vm.RangeLabel})").SemiBold();
                            table.Cell().Text(vm.ApiFailedRequestsInRange.ToString());
                            table.Cell().Text($"API Rate Limited ({vm.RangeLabel})").SemiBold();
                            table.Cell().Text(vm.ApiRateLimitedInRange.ToString());
                        });

                        col.Item().Text("Locked Accounts").Bold();
                        if (!vm.LockedAccounts.Any())
                        {
                            col.Item().Text("No locked accounts.");
                        }
                        else
                        {
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(2);
                                });

                                table.Cell().Text("Email").Bold();
                                table.Cell().Text("Name").Bold();
                                table.Cell().Text("Failed").Bold();
                                table.Cell().Text("Lockout End").Bold();

                                foreach (var item in vm.LockedAccounts)
                                {
                                    table.Cell().Text(item.Email);
                                    table.Cell().Text(item.Name);
                                    table.Cell().Text(item.AccessFailedCount.ToString());
                                    table.Cell().Text(item.LockoutEnd?.LocalDateTime.ToString("yyyy-MM-dd HH:mm") ?? "-");
                                }
                            });
                        }

                        col.Item().Text("Recent Admin Actions").Bold();
                        if (!vm.RecentAdminActions.Any())
                        {
                            col.Item().Text("No admin actions found.");
                        }
                        else
                        {
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(2);
                                });

                                table.Cell().Text("Timestamp").Bold();
                                table.Cell().Text("User").Bold();
                                table.Cell().Text("Action").Bold();
                                table.Cell().Text("Module").Bold();
                                table.Cell().Text("Target").Bold();

                                foreach (var item in vm.RecentAdminActions)
                                {
                                    table.Cell().Text(item.Timestamp);
                                    table.Cell().Text(item.User);
                                    table.Cell().Text(item.Action);
                                    table.Cell().Text(item.Module);
                                    table.Cell().Text(item.Target);
                                }
                            });
                        }
                    });
                });
            }).GeneratePdf();

            var fileName = $"security-dashboard-{vm.Range}-{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        private async Task<SecurityDashboardViewModel> BuildDashboardViewModelAsync(string range, int failedLoginThreshold, int apiFailureThreshold)
        {
            // CRITICAL: Use UTC to match AuditService logging (which now uses DateTime.UtcNow)
            // This ensures dashboard queries find all recent audit logs without timezone mismatches
            var now = DateTime.UtcNow;
            var normalizedRange = NormalizeRange(range);
            var rangeStart = normalizedRange switch
            {
                "30d" => now.AddDays(-30),
                "7d" => now.AddDays(-7),
                _ => now.AddHours(-24)
            };

            var rangeLabel = normalizedRange switch
            {
                "30d" => "Last 30 Days",
                "7d" => "Last 7 Days",
                _ => "Last 24 Hours"
            };

            var trendStart = normalizedRange switch
            {
                "30d" => now.AddDays(-29).Date,
                "7d" => now.AddDays(-6).Date,
                _ => now.Date
            };

            var role = _tenantService.GetCurrentRole();
            var isSuperAdmin = _tenantService.IsSuperAdmin() || string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase);
            var barangayId = _tenantService.GetCurrentBarangayId();

            var usersWithProfile = from identityUser in _userManager.Users
                                   join businessUser in _context.BusinessUsers on identityUser.Email equals businessUser.Email
                                   where businessUser.IsActive
                                   select new { identityUser, businessUser };

            if (!isSuperAdmin)
            {
                if (!barangayId.HasValue)
                {
                    return new SecurityDashboardViewModel
                    {
                        Role = role,
                        IsSuperAdmin = false,
                        Range = normalizedRange,
                        RangeLabel = rangeLabel,
                        RangeStart = rangeStart,
                        RangeEnd = now,
                        FailedLoginAlertThreshold = Math.Max(1, failedLoginThreshold),
                        ApiFailureAlertThreshold = Math.Max(1, apiFailureThreshold)
                    };
                }

                usersWithProfile = usersWithProfile.Where(x => x.businessUser.BarangayId == barangayId.Value);
            }

            var failedAttemptsTotal = await usersWithProfile.SumAsync(x => x.identityUser.AccessFailedCount);
            var accountsWithFailedAttempts = await usersWithProfile.CountAsync(x => x.identityUser.AccessFailedCount > 0);

            var lockedAccountsQuery = usersWithProfile
                .Where(x => x.identityUser.LockoutEnd.HasValue && x.identityUser.LockoutEnd > DateTimeOffset.UtcNow)
                .OrderByDescending(x => x.identityUser.LockoutEnd);

            var lockedAccountsCount = await lockedAccountsQuery.CountAsync();
            var lockedAccounts = await lockedAccountsQuery
                .Take(10)
                .Select(x => new LockedAccountItem
                {
                    Email = x.identityUser.Email ?? string.Empty,
                    Name = x.businessUser.FullName,
                    LockoutEnd = x.identityUser.LockoutEnd,
                    AccessFailedCount = x.identityUser.AccessFailedCount
                })
                .ToListAsync();

            var auditQuery = _context.AuditLogs.Where(a => a.IsActive);
            if (!isSuperAdmin)
            {
                var scopedBarangayId = barangayId!.Value;
                auditQuery = auditQuery.Where(a => _context.BusinessUsers.Any(u => u.Id == a.UserId && u.BarangayId == scopedBarangayId));
            }

            var failedLoginEventsLast24Hours = await auditQuery.CountAsync(a =>
                a.CreatedAt >= rangeStart
                && (
                    (a.Action != null && a.Action.ToLower().Contains("failed"))
                    || (a.Description != null && a.Description.ToLower().Contains("invalid email or password"))
                    || (a.Description != null && a.Description.ToLower().Contains("locked out"))
                ));

            var adminRoleSet = new[] { "super_admin", "barangay_admin", "admin" };
            var recentAdminActionsQuery = from log in auditQuery
                                          join businessUser in _context.BusinessUsers on log.UserId equals businessUser.Id
                                          where adminRoleSet.Contains((businessUser.Role ?? string.Empty).ToLower())
                                          orderby log.CreatedAt descending
                                          select new AdminActionItem
                                          {
                                              Id = log.Id,
                                              Timestamp = log.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                                              User = businessUser.FullName,
                                              Action = log.Action,
                                              Module = log.Module,
                                              Target = log.TargetName ?? "",
                                              Description = log.Description ?? ""
                                          };

            var recentAdminActions = await recentAdminActionsQuery.Take(15).ToListAsync();

            var apiUsageQuery = auditQuery.Where(a =>
                a.CreatedAt >= rangeStart
                && a.TargetType == "HttpAction"
                && (
                    (a.Module != null && a.Module.ToLower().Contains("api"))
                    || (a.Description != null && a.Description.ToLower().Contains("/api/"))
                ));

            var apiRequestsLast24Hours = await apiUsageQuery.CountAsync();
            var apiFailedRequestsLast24Hours = await apiUsageQuery.CountAsync(a =>
                (a.Action != null && a.Action.EndsWith("_FAILED"))
                || (a.Description != null && (a.Description.Contains("status 4") || a.Description.Contains("status 5"))));
            var apiRateLimitedLast24Hours = await apiUsageQuery.CountAsync(a => a.Description != null && a.Description.Contains("status 429"));

            var topApiEndpoints = await apiUsageQuery
                .GroupBy(a => a.TargetName ?? "unknown")
                .Select(g => new ApiEndpointUsageItem
                {
                    Endpoint = g.Key,
                    Count = g.Count(),
                    FailedCount = g.Count(a => (a.Action != null && a.Action.EndsWith("_FAILED"))
                        || (a.Description != null && (a.Description.Contains("status 4") || a.Description.Contains("status 5")))),
                    FailureRatePercent = g.Count() > 0
                        ? (g.Count(a => (a.Action != null && a.Action.EndsWith("_FAILED"))
                            || (a.Description != null && (a.Description.Contains("status 4") || a.Description.Contains("status 5")))) * 100.0 / g.Count())
                        : 0
                })
                .OrderByDescending(x => x.Count)
                .Take(8)
                .ToListAsync();

            var dailyApiUsage = new List<DailyApiUsageItem>();
            var trendDays = normalizedRange switch
            {
                "30d" => 30,
                "7d" => 7,
                _ => 1
            };

            for (var i = 0; i < trendDays; i++)
            {
                var date = trendStart.AddDays(i);
                var next = date.AddDays(1);

                var dayCount = await apiUsageQuery.CountAsync(a => a.CreatedAt >= date && a.CreatedAt < next);
                var dayFailedCount = await apiUsageQuery.CountAsync(a =>
                    a.CreatedAt >= date
                    && a.CreatedAt < next
                    && ((a.Action != null && a.Action.EndsWith("_FAILED"))
                        || (a.Description != null && (a.Description.Contains("status 4") || a.Description.Contains("status 5")))));

                dailyApiUsage.Add(new DailyApiUsageItem
                {
                    Date = normalizedRange == "24h" ? "24h" : date.ToString("MMM d"),
                    Count = dayCount,
                    FailedCount = dayFailedCount
                });
            }

            var apiFailureRatePercent = apiRequestsLast24Hours > 0
                ? (apiFailedRequestsLast24Hours * 100.0 / apiRequestsLast24Hours)
                : 0;

            // ===== NEW COMPREHENSIVE METRICS =====
            
            // MFA Failures
            var mfaFailures = await auditQuery.CountAsync(a =>
                a.CreatedAt >= rangeStart
                && (a.Description!.ToLower().Contains("mfa") || a.Description!.ToLower().Contains("otp")));

            // Successful Logins
            var successfulLogins = await auditQuery.CountAsync(a =>
                a.CreatedAt >= rangeStart
                && a.Action != null
                && (a.Action.ToLower().Contains("login") && !a.Action.ToLower().Contains("failed")));

            // Authorization Denials
            var authDenials = await auditQuery.CountAsync(a =>
                a.CreatedAt >= rangeStart
                && (a.Action != null && a.Action.ToLower().Contains("denied"))
                || (a.Description != null && a.Description.ToLower().Contains("access denied")));

            // Data Modifications
            var docsCreated = await auditQuery.CountAsync(a =>
                a.CreatedAt >= rangeStart
                && (a.Module == "KnowledgeRepository" || a.Module == "Documents")
                && (a.Action == "Created" || a.Action == "Uploaded"));

            var docsModified = await auditQuery.CountAsync(a =>
                a.CreatedAt >= rangeStart
                && (a.Module == "KnowledgeRepository" || a.Module == "Documents")
                && a.Action == "Updated");

            var docsDeleted = await auditQuery.CountAsync(a =>
                a.CreatedAt >= rangeStart
                && (a.Module == "KnowledgeRepository" || a.Module == "Documents")
                && a.Action == "Deleted");

            var policiesModified = await auditQuery.CountAsync(a =>
                a.CreatedAt >= rangeStart
                && a.Module == "Policies"
                && (a.Action == "Created" || a.Action == "Updated"));

            var usersModified = await auditQuery.CountAsync(a =>
                a.CreatedAt >= rangeStart
                && (a.Module == "Users" || a.Module == "UserManagement")
                && (a.Action == "Created" || a.Action == "Updated"));

            // Recent Security Events (MFA/Auth failures, denials)
            var securityEvents = await auditQuery
                .Where(a => a.CreatedAt >= rangeStart
                    && (a.Action!.ToLower().Contains("failed")
                        || a.Action!.ToLower().Contains("denied")
                        || a.Description!.ToLower().Contains("mfa")
                        || a.Description!.ToLower().Contains("locked")))
                .OrderByDescending(a => a.CreatedAt)
                .Take(10)
                .Select(a => new SecurityEventItem
                {
                    Id = a.Id,
                    Timestamp = a.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    EventType = a.Action ?? "Unknown",
                    Severity = a.Description != null && (a.Description.Contains("locked") || a.Description.Contains("denied")) ? "Critical" : "Warning",
                    Description = a.Description ?? "",
                    User = a.UserName ?? a.UserEmail ?? "System"
                })
                .ToListAsync();

            // Recent Data Changes
            var dataChanges = await auditQuery
                .Where(a => a.CreatedAt >= rangeStart
                    && (a.Action == "Created" || a.Action == "Updated" || a.Action == "Deleted")
                    && (a.Module == "KnowledgeRepository" || a.Module == "Policies" || a.Module == "Users"))
                .OrderByDescending(a => a.CreatedAt)
                .Take(15)
                .Select(a => new DataChangeItem
                {
                    Id = a.Id,
                    Timestamp = a.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    Action = a.Action ?? "Unknown",
                    EntityType = a.TargetType ?? a.Module ?? "Unknown",
                    EntityName = a.TargetName ?? "",
                    ChangedBy = a.UserName ?? a.UserEmail ?? "System",
                    Details = a.Description ?? ""
                })
                .ToListAsync();

            // Active Users in Range
            var activeUsers = await auditQuery
                .Where(a => a.CreatedAt >= rangeStart && a.UserId.HasValue)
                .GroupBy(a => new { a.UserId, a.UserEmail, a.UserName })
                .Select(g => new UserActivityItem
                {
                    UserId = g.Key.UserId ?? 0,
                    Email = g.Key.UserEmail ?? "",
                    Name = g.Key.UserName ?? "Unknown",
                    ActionCount = g.Count(),
                    LastActive = g.Max(x => x.CreatedAt).ToString("yyyy-MM-dd HH:mm:ss"),
                    Role = ""
                })
                .OrderByDescending(x => x.ActionCount)
                .Take(10)
                .ToListAsync();

            // Module Activity Breakdown
            var moduleActivity = await auditQuery
                .Where(a => a.CreatedAt >= rangeStart)
                .GroupBy(a => a.Module)
                .Select(g => new ModuleActivityItem
                {
                    Module = g.Key ?? "Unknown",
                    EventCount = g.Count(),
                    CreatedCount = g.Count(x => x.Action == "Created" || x.Action == "Uploaded"),
                    ModifiedCount = g.Count(x => x.Action == "Updated"),
                    DeletedCount = g.Count(x => x.Action == "Deleted")
                })
                .OrderByDescending(x => x.EventCount)
                .Take(10)
                .ToListAsync();

            // System Alerts (combination of critical events)
            var systemAlerts = new List<SystemAlertItem>();
            if (failedLoginEventsLast24Hours > Math.Max(1, failedLoginThreshold))
            {
                systemAlerts.Add(new SystemAlertItem
                {
                    Id = 1,
                    Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                    Title = "High Failed Login Attempts",
                    Severity = "Critical",
                    Message = $"{failedLoginEventsLast24Hours} failed login attempts detected in {rangeLabel}"
                });
            }

            if (lockedAccountsCount > 0)
            {
                systemAlerts.Add(new SystemAlertItem
                {
                    Id = 2,
                    Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                    Title = "Locked User Accounts",
                    Severity = "Warning",
                    Message = $"{lockedAccountsCount} user accounts are currently locked"
                });
            }

            if (mfaFailures > 5)
            {
                systemAlerts.Add(new SystemAlertItem
                {
                    Id = 3,
                    Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                    Title = "MFA Failures Detected",
                    Severity = "Warning",
                    Message = $"{mfaFailures} MFA verification failures in {rangeLabel}"
                });
            }

            if (apiFailedRequestsLast24Hours > Math.Max(1, apiFailureThreshold))
            {
                systemAlerts.Add(new SystemAlertItem
                {
                    Id = 4,
                    Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                    Title = "API Failures Spike",
                    Severity = "Critical",
                    Message = $"{apiFailedRequestsLast24Hours} API request failures in {rangeLabel}"
                });
            }

            // Count new users created in range
            var newUsersInRange = await _context.BusinessUsers.CountAsync(u =>
                u.IsActive && u.CreatedAt >= rangeStart);

            // Count inactive accounts (locked out)
            var inactiveAccountsCount = await _userManager.Users.CountAsync(u =>
                u.LockoutEnd.HasValue && u.LockoutEnd > DateTime.UtcNow);

            var vm = new SecurityDashboardViewModel
            {
                Role = role,
                IsSuperAdmin = isSuperAdmin,
                Range = normalizedRange,
                RangeLabel = rangeLabel,
                RangeStart = rangeStart,
                RangeEnd = now,
                FailedLoginAttemptsTotal = failedAttemptsTotal,
                AccountsWithFailedAttempts = accountsWithFailedAttempts,
                FailedLoginEventsInRange = failedLoginEventsLast24Hours,
                FailedLoginAlertThreshold = Math.Max(1, failedLoginThreshold),
                IsFailedLoginAlert = failedLoginEventsLast24Hours >= Math.Max(1, failedLoginThreshold),
                LockedAccountsCount = lockedAccountsCount,
                LockedAccounts = lockedAccounts,
                MfaFailuresInRange = mfaFailures,
                SuccessfulLoginsInRange = successfulLogins,
                TotalLoginAttemptsInRange = failedLoginEventsLast24Hours + successfulLogins,
                ApiRequestsInRange = apiRequestsLast24Hours,
                ApiFailedRequestsInRange = apiFailedRequestsLast24Hours,
                ApiRateLimitedInRange = apiRateLimitedLast24Hours,
                ApiFailureAlertThreshold = Math.Max(1, apiFailureThreshold),
                IsApiFailureAlert = apiFailedRequestsLast24Hours >= Math.Max(1, apiFailureThreshold),
                ApiFailureRatePercent = Math.Round(apiFailureRatePercent, 2),
                AuthorizationDenialsInRange = authDenials,
                SuspiciousActivitiesInRange = mfaFailures + authDenials,
                RecentSecurityEvents = securityEvents,
                DocumentsCreatedInRange = docsCreated,
                DocumentsModifiedInRange = docsModified,
                DocumentsDeletedInRange = docsDeleted,
                PoliciesModifiedInRange = policiesModified,
                UsersModifiedInRange = usersModified,
                RecentDataChanges = dataChanges,
                ActiveUsersInRange = activeUsers.Count,
                TopActiveUsers = activeUsers,
                NewUsersInRange = newUsersInRange,
                InactiveAccountsCount = inactiveAccountsCount,
                CriticalAlertsCount = systemAlerts.Count(x => x.Severity == "Critical"),
                WarningAlertsCount = systemAlerts.Count(x => x.Severity == "Warning"),
                RecentAlerts = systemAlerts.OrderByDescending(x => x.Timestamp).Take(8).ToList(),
                RecentAdminActions = recentAdminActions,
                TopApiEndpoints = topApiEndpoints,
                DailyApiUsage = dailyApiUsage,
                ModuleActivityBreakdown = moduleActivity
            };

            return vm;
        }

        private static string NormalizeRange(string? range)
        {
            var value = (range ?? string.Empty).Trim().ToLowerInvariant();
            return value switch
            {
                "30d" => "30d",
                "7d" => "7d",
                _ => "24h"
            };
        }

        private static string EscapeCsv(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var escaped = value.Replace("\"", "\"\"");
            return $"\"{escaped}\"";
        }

        private static string BuildCsv(SecurityDashboardViewModel vm)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Security Dashboard Report");
            sb.AppendLine($"Range,{EscapeCsv(vm.RangeLabel)}");
            sb.AppendLine($"Generated,{EscapeCsv(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))}");
            sb.AppendLine();

            sb.AppendLine("Metric,Value");
            sb.AppendLine($"Failed Login Attempts Current Counters,{vm.FailedLoginAttemptsTotal}");
            sb.AppendLine($"Failed Login Signals In Range,{vm.FailedLoginEventsInRange}");
            sb.AppendLine($"Locked Accounts,{vm.LockedAccountsCount}");
            sb.AppendLine($"API Requests In Range,{vm.ApiRequestsInRange}");
            sb.AppendLine($"API Failed Requests In Range,{vm.ApiFailedRequestsInRange}");
            sb.AppendLine($"API Rate Limited In Range,{vm.ApiRateLimitedInRange}");
            sb.AppendLine();

            sb.AppendLine("Locked Accounts");
            sb.AppendLine("Email,Name,AccessFailedCount,LockoutEnd");
            foreach (var item in vm.LockedAccounts)
            {
                sb.AppendLine($"{EscapeCsv(item.Email)},{EscapeCsv(item.Name)},{item.AccessFailedCount},{EscapeCsv(item.LockoutEnd?.LocalDateTime.ToString("yyyy-MM-dd HH:mm") ?? "-")}");
            }
            sb.AppendLine();

            sb.AppendLine("Recent Admin Actions");
            sb.AppendLine("Timestamp,User,Action,Module,Target,Description");
            foreach (var item in vm.RecentAdminActions)
            {
                sb.AppendLine($"{EscapeCsv(item.Timestamp)},{EscapeCsv(item.User)},{EscapeCsv(item.Action)},{EscapeCsv(item.Module)},{EscapeCsv(item.Target)},{EscapeCsv(item.Description)}");
            }

            return sb.ToString();
        }
    }
}
