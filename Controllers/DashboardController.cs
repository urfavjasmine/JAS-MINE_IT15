using JAS_MINE_IT15.Data;
using JAS_MINE_IT15.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace JAS_MINE_IT15.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public DashboardController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        #region Helper Methods

        /// <summary>
        /// Gets the current user's role from session.
        /// </summary>
        private string GetCurrentRole()
        {
            return User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role || c.Type == "role")?.Value
                ?? HttpContext.Session.GetString("Role")
                ?? "";
        }

        /// <summary>
        /// Gets the current user's BarangayId from session.
        /// </summary>
        private int? GetCurrentBarangayId()
        {
            var claimValue = User.Claims.FirstOrDefault(c => c.Type == "BarangayId")?.Value;
            if (int.TryParse(claimValue, out var claimId))
                return claimId;

            var barangayIdStr = HttpContext.Session.GetString("BarangayId");
            if (int.TryParse(barangayIdStr, out var id))
                return id;

            var userIdStr = HttpContext.Session.GetString("UserId");
            if (int.TryParse(userIdStr, out var userId))
            {
                return _context.BusinessUsers
                    .Where(u => u.IsActive && u.Id == userId)
                    .Select(u => u.BarangayId)
                    .FirstOrDefault();
            }

            return null;
        }

        private string GetRoleLabel()
        {
            return GetCurrentRole() switch
            {
                "super_admin" => "Super Admin",
                "barangay_admin" => "Barangay Administrator",
                "user" => "User",
                _ => "User"
            };
        }

        private string GetCurrentBarangayName()
        {
            var barangayId = GetCurrentBarangayId();
            if (!barangayId.HasValue)
                return string.Empty;

            return _context.Barangays
                .Where(b => b.IsActive && b.Id == barangayId.Value)
                .Select(b => b.Name)
                .FirstOrDefault() ?? string.Empty;
        }

        /// <summary>
        /// Checks if current user is super_admin.
        /// </summary>
        private bool IsSuperAdmin()
        {
            return GetCurrentRole() == "super_admin";
        }

        /// <summary>
        /// Checks if current user has view-only access (council_member).
        /// </summary>
        private bool IsViewOnly()
        {
            return GetCurrentRole() == "council_member";
        }

        /// <summary>
        /// Checks if current user can create/edit/delete (not council_member).
        /// </summary>
        private bool CanModify()
        {
            var role = GetCurrentRole();
            return role == "super_admin" || role == "barangay_admin" || role == "barangay_secretary" || role == "barangay_staff";
        }

        #endregion

        #region System Dashboard (super_admin only)

        /// <summary>
        /// System-wide dashboard for super_admin with access to all barangays.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "super_admin")]
        public async Task<IActionResult> System()
        {
            var vm = new SystemDashboardViewModel
            {
                Role = GetCurrentRole(),
                RoleLabel = GetRoleLabel()
            };

            // System-wide stats
            vm.TotalBarangays = await _context.Barangays.CountAsync(b => b.IsActive);
            vm.ActiveBarangays = vm.TotalBarangays; // Will be adjusted below
            vm.TotalUsers = await _context.BusinessUsers.CountAsync(u => u.IsActive);
            vm.TotalDocuments = await _context.KnowledgeDocuments.CountAsync(d => d.IsActive);
            vm.TotalPolicies = await _context.Policies.CountAsync(p => p.IsActive);
            vm.TotalBestPractices = await _context.BestPractices.CountAsync(bp => bp.IsActive);
            vm.TotalLessonsLearned = await _context.LessonsLearned.CountAsync(ll => ll.IsActive);
            vm.TotalAnnouncements = await _context.Announcements.CountAsync(a => a.IsActive);

            // Subscription stats
            vm.ActiveSubscriptions = await _context.BarangaySubscriptions
                .CountAsync(s => s.IsActive && s.Status == "Active");
            vm.ExpiredSubscriptions = await _context.BarangaySubscriptions
                .CountAsync(s => s.IsActive && s.Status == "Expired");
            vm.PendingSubscriptions = await _context.BarangaySubscriptions
                .CountAsync(s => s.IsActive && s.Status == "Pending");

            // Revenue
            vm.TotalRevenue = await _context.SubscriptionPayments
                .Where(p => p.IsActive && (p.Status == "Approved" || p.Status == "Paid"))
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;

            var thisMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            vm.MonthlyRevenue = await _context.SubscriptionPayments
                .Where(p => p.IsActive && (p.Status == "Approved" || p.Status == "Paid") && p.PaymentDate >= thisMonth)
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;

            // Monthly revenue breakdown (last 12 months)
            var twelveMonthsAgo = DateTime.Now.AddMonths(-11);
            var startMonth = new DateTime(twelveMonthsAgo.Year, twelveMonthsAgo.Month, 1);
            var monthlyData = await _context.SubscriptionPayments
                .Where(p => p.IsActive && (p.Status == "Approved" || p.Status == "Paid") && p.PaymentDate >= startMonth)
                .GroupBy(p => new { p.PaymentDate.Year, p.PaymentDate.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(p => p.Amount) })
                .OrderBy(g => g.Year).ThenBy(g => g.Month)
                .ToListAsync();

            // Build full 12 months (fill gaps with 0)
            for (int i = 0; i < 12; i++)
            {
                var d = startMonth.AddMonths(i);
                var match = monthlyData.FirstOrDefault(m => m.Year == d.Year && m.Month == d.Month);
                vm.MonthlyRevenueData.Add(new MonthlyRevenueItem
                {
                    Month = d.ToString("MMM yyyy"),
                    Amount = match?.Total ?? 0m
                });
            }

            // Pending approvals
            vm.PendingDocuments = await _context.KnowledgeDocuments
                .CountAsync(d => d.IsActive && d.Status == "pending");
            vm.PendingPolicies = await _context.Policies
                .CountAsync(p => p.IsActive && p.Status == "pending");
            vm.PendingPayments = await _context.SubscriptionPayments
                .CountAsync(p => p.IsActive && p.Status == "PendingVerification");

            // Barangay summaries (activity report)
            var barangays = await _context.Barangays.Where(b => b.IsActive).ToListAsync();
            foreach (var b in barangays)
            {
                var sub = await _context.BarangaySubscriptions
                    .Include(s => s.Plan)
                    .Where(s => s.BarangayId == b.Id && s.IsActive)
                    .OrderByDescending(s => s.EndDate)
                    .FirstOrDefaultAsync();

                vm.BarangaySummaries.Add(new BarangaySummaryItem
                {
                    BarangayId = b.Id,
                    BarangayName = b.Name,
                    TotalUsers = await _context.BusinessUsers.CountAsync(u => u.IsActive && u.BarangayId == b.Id),
                    TotalDocuments = await _context.KnowledgeDocuments.CountAsync(d => d.IsActive && d.BarangayId == b.Id),
                    TotalPolicies = await _context.Policies.CountAsync(p => p.IsActive && p.BarangayId == b.Id),
                    TotalLessonsLearned = await _context.LessonsLearned.CountAsync(l => l.IsActive && l.BarangayId == b.Id),
                    TotalBestPractices = await _context.BestPractices.CountAsync(bp => bp.IsActive && bp.BarangayId == b.Id),
                    TotalAnnouncements = await _context.Announcements.CountAsync(a => a.IsActive && a.BarangayId == b.Id),
                    PlanName = sub?.Plan?.Name ?? "None",
                    SubscriptionStatus = sub?.Status ?? "None"
                });
            }

            // Subscription report (per barangay)
            var allSubs = await _context.BarangaySubscriptions
                .Where(s => s.IsActive)
                .Include(s => s.Barangay)
                .Include(s => s.Plan)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            foreach (var sub in allSubs)
            {
                var lastPayment = await _context.SubscriptionPayments
                    .Where(p => p.IsActive && p.SubscriptionId == sub.Id && (p.Status == "Approved" || p.Status == "Paid"))
                    .OrderByDescending(p => p.PaymentDate)
                    .FirstOrDefaultAsync();

                var latestInvoice = await _context.Invoices
                    .Where(i => i.IsActive && i.SubscriptionId == sub.Id)
                    .OrderByDescending(i => i.IssuedAt)
                    .FirstOrDefaultAsync();

                var paymentStatus = latestInvoice?.Status ?? "No Invoice";

                vm.SubscriptionReport.Add(new SubscriptionReportItem
                {
                    BarangayName = sub.Barangay?.Name ?? "Unknown",
                    PlanName = sub.Plan?.Name ?? "Unknown",
                    PaymentStatus = paymentStatus,
                    LastPaymentDate = lastPayment?.PaymentDate.ToString("yyyy-MM-dd") ?? "N/A",
                    ExpiryDate = sub.EndDate.ToString("yyyy-MM-dd"),
                    Amount = sub.Plan?.Price ?? 0m
                });
            }

            // Inactive barangays (no login in last 30 days or no transactions)
            var thirtyDaysAgo = DateTime.Now.AddDays(-30);
            foreach (var b in barangays)
            {
                var hasRecentLogin = await _context.AuditLogs
                    .AnyAsync(a => a.IsActive && a.Action == "Login" && a.CreatedAt >= thirtyDaysAgo
                        && _context.BusinessUsers.Any(u => u.Id == a.UserId && u.BarangayId == b.Id));

                var hasRecentTransaction = await _context.AuditLogs
                    .AnyAsync(a => a.IsActive && a.CreatedAt >= thirtyDaysAgo && a.Action != "Login"
                        && _context.BusinessUsers.Any(u => u.Id == a.UserId && u.BarangayId == b.Id));

                if (!hasRecentLogin && !hasRecentTransaction)
                {
                    var lastActivity = await _context.AuditLogs
                        .Where(a => a.IsActive && _context.BusinessUsers.Any(u => u.Id == a.UserId && u.BarangayId == b.Id))
                        .OrderByDescending(a => a.CreatedAt)
                        .Select(a => a.CreatedAt)
                        .FirstOrDefaultAsync();

                    vm.InactiveBarangays.Add(new InactiveBarangayItem
                    {
                        BarangayId = b.Id,
                        BarangayName = b.Name,
                        LastActivityDate = lastActivity != default ? lastActivity.ToString("yyyy-MM-dd HH:mm") : "Never",
                        Reason = !hasRecentLogin ? "No login in last 30 days" : "No transactions recorded"
                    });
                }
            }

            // Count active vs inactive barangays
            vm.ActiveBarangays = vm.TotalBarangays - vm.InactiveBarangays.Count;

            // Recent activity (last 10)
            vm.RecentActivity = await _context.AuditLogs
                .Where(a => a.IsActive)
                .OrderByDescending(a => a.CreatedAt)
                .Take(10)
                .Select(a => new ActivityItem
                {
                    Timestamp = a.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                    User = FormatActivityUser(a.UserName, a.UserEmail),
                    Action = FormatActivityAction(a.Action),
                    Module = FormatActivityModule(a.Module),
                    Target = FormatActivityTarget(a.TargetName)
                })
                .ToListAsync();

            return View(vm);
        }

        #endregion

        private static string FormatActivityUser(string? userName, string? userEmail)
        {
            if (!string.IsNullOrWhiteSpace(userName))
            {
                return userName.Trim();
            }

            if (!string.IsNullOrWhiteSpace(userEmail))
            {
                var trimmedEmail = userEmail.Trim();
                var normalizedEmail = trimmedEmail.ToLowerInvariant();

                if (normalizedEmail == "system_admin@jasmine.gov.ph")
                {
                    return "System Admin";
                }

                if (trimmedEmail.Contains('*'))
                {
                    return "Unknown User";
                }

                return trimmedEmail;
            }

            return "System";
        }

        private static string FormatActivityAction(string? action)
        {
            if (string.IsNullOrWhiteSpace(action))
            {
                return "Action";
            }

            var trimmed = action.Trim();
            return trimmed switch
            {
                "GET" => "View",
                "POST" => "Submit",
                "PUT" => "Update",
                "PATCH" => "Update",
                "DELETE" => "Delete",
                _ => HumanizeToken(trimmed)
            };
        }

        private static string FormatActivityModule(string? module)
        {
            if (string.IsNullOrWhiteSpace(module))
            {
                return "System";
            }

            var trimmed = module.Trim();
            if (trimmed.EndsWith("Controller", StringComparison.OrdinalIgnoreCase))
            {
                trimmed = trimmed[..^"Controller".Length];
            }

            return trimmed switch
            {
                "PasswordRequests" => "Password Reset Requests",
                "KnowledgeRepository" => "Knowledge Repository",
                "KnowledgeDiscussions" => "Knowledge Discussions",
                "SubscriptionPayments" => "Subscription Payments",
                "BarangaySubscriptions" => "Barangay Subscriptions",
                _ => HumanizeToken(trimmed)
            };
        }

        private static string FormatActivityTarget(string? target)
        {
            if (string.IsNullOrWhiteSpace(target))
            {
                return string.Empty;
            }

            var normalized = HumanizeToken(target.Trim());
            if (string.Equals(normalized, "Requests", StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            return normalized;
        }

        private static string HumanizeToken(string value)
        {
            var cleaned = value.Replace("_", " ").Replace("-", " ");
            if (cleaned.Contains('/'))
            {
                var parts = cleaned.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                return string.Join(" / ", parts.Select(SplitPascalCase).Select(NormalizeLabel));
            }

            return NormalizeLabel(SplitPascalCase(cleaned));
        }

        private static string SplitPascalCase(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var builder = new System.Text.StringBuilder(value.Length + 8);
            for (var i = 0; i < value.Length; i++)
            {
                var current = value[i];
                if (i > 0 && char.IsUpper(current) && (char.IsLower(value[i - 1]) || char.IsDigit(value[i - 1])))
                {
                    builder.Append(' ');
                }

                builder.Append(current);
            }

            return builder.ToString();
        }

        private static string NormalizeLabel(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var titled = global::System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(value.ToLowerInvariant());
            return NormalizeAcronyms(titled);
        }

        private static string NormalizeAcronyms(string value)
        {
            var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < parts.Length; i++)
            {
                parts[i] = parts[i].ToLowerInvariant() switch
                {
                    "mfa" => "MFA",
                    "ip" => "IP",
                    "id" => "ID",
                    "api" => "API",
                    "otp" => "OTP",
                    _ => parts[i]
                };
            }

            return string.Join(" ", parts);
        }

        #region System Monitoring (super_admin only)

        /// <summary>
        /// System monitoring dashboard with aggregate statistics across all barangays.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "super_admin")]
        public async Task<IActionResult> SystemMonitoring()
        {
            var vm = new SystemMonitoringDashboardViewModel
            {
                Role = GetCurrentRole()
            };

            var today = DateTime.Today;
            var thisMonth = new DateTime(today.Year, today.Month, 1);
            var lastMonth = thisMonth.AddMonths(-1);

            // ========== AGGREGATE STATS ==========
            vm.TotalBarangays = await _context.Barangays.CountAsync(b => b.IsActive);
            vm.TotalUsers = await _context.BusinessUsers.CountAsync(u => u.IsActive);
            vm.TotalDocuments = await _context.KnowledgeDocuments.CountAsync(d => d.IsActive);
            vm.TotalPolicies = await _context.Policies.CountAsync(p => p.IsActive);
            vm.TotalLessonsLearned = await _context.LessonsLearned.CountAsync(l => l.IsActive);
            vm.TotalBestPractices = await _context.BestPractices.CountAsync(bp => bp.IsActive);

            // Growth this month
            vm.NewBarangaysThisMonth = await _context.Barangays.CountAsync(b => b.IsActive && b.CreatedAt >= thisMonth);
            vm.NewUsersThisMonth = await _context.BusinessUsers.CountAsync(u => u.IsActive && u.CreatedAt >= thisMonth);
            vm.NewDocumentsThisMonth = await _context.KnowledgeDocuments.CountAsync(d => d.IsActive && d.CreatedAt >= thisMonth);
            vm.NewPoliciesThisMonth = await _context.Policies.CountAsync(p => p.IsActive && p.CreatedAt >= thisMonth);
            vm.NewLessonsThisMonth = await _context.LessonsLearned.CountAsync(l => l.IsActive && l.CreatedAt >= thisMonth);
            vm.NewBestPracticesThisMonth = await _context.BestPractices.CountAsync(bp => bp.IsActive && bp.CreatedAt >= thisMonth);

            // ========== PER-BARANGAY SUMMARY ==========
            var barangays = await _context.Barangays.Where(b => b.IsActive).ToListAsync();
            foreach (var b in barangays)
            {
                var subscription = await _context.BarangaySubscriptions
                    .Include(s => s.Plan)
                    .Where(s => s.BarangayId == b.Id && s.IsActive)
                    .OrderByDescending(s => s.EndDate)
                    .FirstOrDefaultAsync();

                vm.BarangaySummaries.Add(new PerBarangaySummaryItem
                {
                    BarangayId = b.Id,
                    BarangayName = b.Name,
                    UserCount = await _context.BusinessUsers.CountAsync(u => u.IsActive && u.BarangayId == b.Id),
                    DocumentCount = await _context.KnowledgeDocuments.CountAsync(d => d.IsActive && d.BarangayId == b.Id),
                    PolicyCount = await _context.Policies.CountAsync(p => p.IsActive && p.BarangayId == b.Id),
                    LessonCount = await _context.LessonsLearned.CountAsync(l => l.IsActive && l.BarangayId == b.Id),
                    BestPracticeCount = await _context.BestPractices.CountAsync(bp => bp.IsActive && bp.BarangayId == b.Id),
                    SubscriptionStatus = subscription?.Status ?? "none",
                    PlanName = subscription?.Plan?.Name ?? "No Plan"
                });
            }

            return View(vm);
        }

        #endregion

        #region Security Monitoring (super_admin only)

        /// <summary>
        /// Security monitoring dashboard showing login activity and user sessions.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "super_admin")]
        public async Task<IActionResult> SecurityMonitoring(string filter = "all", int page = 1)
        {
            var vm = new SecurityMonitoringViewModel
            {
                Role = GetCurrentRole(),
                Filter = filter,
                CurrentPage = page,
                PageSize = 20
            };

            // Get date ranges
            var today = DateTime.Today;
            var last7Days = today.AddDays(-7);
            var last30Days = today.AddDays(-30);

            // ===== TOP STATS (30d) =====
            vm.TotalLoginsLast30Days = await _context.AuditLogs
                .CountAsync(a => a.IsActive && a.Action == "Login" && a.CreatedAt >= last30Days);

            vm.FailedLoginsLast30Days = await _context.AuditLogs
                .CountAsync(a => a.IsActive && (a.Action == "LoginFailed" || a.Action == "FailedLogin") && a.CreatedAt >= last30Days);

            // Active sessions (unique users logged in today)
            vm.ActiveSessions = await _context.AuditLogs
                .Where(a => a.IsActive && a.Action == "Login" && a.CreatedAt >= today && a.UserId != null)
                .Select(a => a.UserId)
                .Distinct()
                .CountAsync();

            // Suspicious activity: IPs with 3+ failed logins in last 7 days
            vm.SuspiciousActivity = await _context.AuditLogs
                .Where(a => a.IsActive && (a.Action == "LoginFailed" || a.Action == "FailedLogin") && a.CreatedAt >= last7Days && a.IpAddress != null)
                .GroupBy(a => a.IpAddress)
                .Where(g => g.Count() >= 3)
                .CountAsync();

            // ===== DAILY LOGIN ACTIVITY (Last 7 Days) with success/fail split =====
            for (int i = 6; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                var nextDate = date.AddDays(1);
                
                var successCount = await _context.AuditLogs
                    .CountAsync(a => a.IsActive && a.Action == "Login" && a.CreatedAt >= date && a.CreatedAt < nextDate);
                var failCount = await _context.AuditLogs
                    .CountAsync(a => a.IsActive && (a.Action == "LoginFailed" || a.Action == "FailedLogin") && a.CreatedAt >= date && a.CreatedAt < nextDate);

                vm.DailyLoginTrend.Add(new DailyLoginItem
                {
                    Date = date.ToString("MMM d"),
                    Count = successCount,
                    FailedCount = failCount
                });
            }

            // ===== LOGINS BY ROLE (30d) =====
            var loginUserIds = await _context.AuditLogs
                .Where(a => a.IsActive && a.Action == "Login" && a.CreatedAt >= last30Days && a.UserId != null)
                .Select(a => a.UserId)
                .ToListAsync();

            var roleGroups = await _context.BusinessUsers
                .Where(u => loginUserIds.Contains(u.Id))
                .GroupBy(u => u.Role)
                .Select(g => new LoginsByRoleItem { Role = g.Key ?? "Unknown", Count = g.Count() })
                .ToListAsync();

            // Calculate percentages
            var totalRoleLogins = roleGroups.Sum(r => r.Count);
            foreach (var r in roleGroups)
            {
                r.Percentage = totalRoleLogins > 0 ? Math.Round((r.Count * 100.0 / totalRoleLogins), 0) : 0;
                r.Role = r.Role.Replace("_", " ").ToUpper() switch
                {
                    "BARANGAY ADMIN" => "Barangay Admin",
                    "BARANGAY SECRETARY" => "Secretary",
                    "BARANGAY STAFF" => "Staff",
                    "SUPER ADMIN" => "Super Admin",
                    "COUNCIL MEMBER" => "Council Member",
                    _ => r.Role
                };
            }
            vm.LoginsByRole = roleGroups.OrderByDescending(r => r.Count).ToList();

            // ===== LOGIN ACTIVITY LOG =====
            var query = _context.AuditLogs
                .Where(a => a.IsActive && (a.Action == "Login" || a.Action == "LoginFailed" || a.Action == "FailedLogin" || a.Action == "Logout"));

            if (filter == "successful")
                query = query.Where(a => a.Action == "Login");
            else if (filter == "failed")
                query = query.Where(a => a.Action == "LoginFailed" || a.Action == "FailedLogin");
            else if (filter == "today")
                query = query.Where(a => a.CreatedAt >= today);

            // Count total for pagination
            vm.TotalRecords = await query.CountAsync();

            // Get paginated login activity with user and barangay info
            var loginActivities = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * vm.PageSize)
                .Take(vm.PageSize)
                .Select(a => new LoginActivityItem
                {
                    Id = a.Id,
                    UserEmail = a.UserEmail ?? "Unknown",
                    UserName = a.UserName ?? "Unknown",
                    Action = a.Action,
                    IpAddress = a.IpAddress ?? "N/A",
                    Timestamp = a.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    UserId = a.UserId
                })
                .ToListAsync();

            // Get barangay info for each user
            foreach (var activity in loginActivities)
            {
                if (activity.UserId.HasValue)
                {
                    var user = await _context.BusinessUsers
                        .Where(u => u.Id == activity.UserId.Value)
                        .Select(u => new { u.BarangayId, u.BarangayName })
                        .FirstOrDefaultAsync();

                    activity.BarangayName = user?.BarangayName ?? "N/A";
                }
                else
                {
                    activity.BarangayName = "N/A";
                }
            }

            vm.LoginActivities = loginActivities;

            return View(vm);
        }

        #endregion

        #region System Analytics (super_admin only)

        /// <summary>
        /// System analytics page with usage statistics and trends.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "super_admin")]
        public async Task<IActionResult> SystemAnalytics()
        {
            var vm = new SystemAnalyticsViewModel
            {
                Role = GetCurrentRole()
            };

            var today = DateTime.Today;
            var thisMonth = new DateTime(today.Year, today.Month, 1);
            var lastMonth = thisMonth.AddMonths(-1);
            var last30Days = today.AddDays(-30);

            // ========== TOP STATS ==========
            // Active Users Today (users with activity today)
            vm.ActiveUsersToday = await _context.AuditLogs
                .Where(a => a.IsActive && a.CreatedAt >= today && a.UserId != null)
                .Select(a => a.UserId)
                .Distinct()
                .CountAsync();

            // Active Barangays (barangays with any user activity today)
            var activeBarangayUsers = await _context.AuditLogs
                .Where(a => a.IsActive && a.CreatedAt >= today && a.UserId != null)
                .Select(a => a.UserId)
                .Distinct()
                .ToListAsync();
            vm.ActiveBarangays = await _context.BusinessUsers
                .Where(u => activeBarangayUsers.Contains(u.Id) && u.BarangayId != null)
                .Select(u => u.BarangayId)
                .Distinct()
                .CountAsync();

            // Uploads This Month (documents + policies + lessons + best practices)
            var docsThisMonth = await _context.KnowledgeDocuments.CountAsync(d => d.IsActive && d.CreatedAt >= thisMonth);
            var policiesThisMonth = await _context.Policies.CountAsync(p => p.IsActive && p.CreatedAt >= thisMonth);
            var lessonsThisMonth = await _context.LessonsLearned.CountAsync(l => l.IsActive && l.CreatedAt >= thisMonth);
            var practicesThisMonth = await _context.BestPractices.CountAsync(bp => bp.IsActive && bp.CreatedAt >= thisMonth);
            vm.UploadsThisMonth = docsThisMonth + policiesThisMonth + lessonsThisMonth + practicesThisMonth;

            // Last month uploads for MoM calculation
            var docsLastMonth = await _context.KnowledgeDocuments.CountAsync(d => d.IsActive && d.CreatedAt >= lastMonth && d.CreatedAt < thisMonth);
            var policiesLastMonth = await _context.Policies.CountAsync(p => p.IsActive && p.CreatedAt >= lastMonth && p.CreatedAt < thisMonth);
            var lessonsLastMonth = await _context.LessonsLearned.CountAsync(l => l.IsActive && l.CreatedAt >= lastMonth && l.CreatedAt < thisMonth);
            var practicesLastMonth = await _context.BestPractices.CountAsync(bp => bp.IsActive && bp.CreatedAt >= lastMonth && bp.CreatedAt < thisMonth);
            var uploadsLastMonth = docsLastMonth + policiesLastMonth + lessonsLastMonth + practicesLastMonth;

            // MoM Growth
            vm.MoMGrowth = uploadsLastMonth > 0 
                ? Math.Round(((vm.UploadsThisMonth - uploadsLastMonth) * 100.0 / uploadsLastMonth), 0)
                : 0;

            // ========== UPLOADS BY TYPE (totals) ==========
            vm.TotalDocuments = await _context.KnowledgeDocuments.CountAsync(d => d.IsActive);
            vm.TotalPolicies = await _context.Policies.CountAsync(p => p.IsActive);
            vm.TotalLessonsLearned = await _context.LessonsLearned.CountAsync(l => l.IsActive);
            vm.TotalBestPractices = await _context.BestPractices.CountAsync(bp => bp.IsActive);

            // ========== MONTHLY DOCUMENT UPLOADS (Last 7 months) ==========
            for (int i = 6; i >= 0; i--)
            {
                var monthStart = thisMonth.AddMonths(-i);
                var monthEnd = monthStart.AddMonths(1);

                var docs = await _context.KnowledgeDocuments.CountAsync(d => d.IsActive && d.CreatedAt >= monthStart && d.CreatedAt < monthEnd);
                var policies = await _context.Policies.CountAsync(p => p.IsActive && p.CreatedAt >= monthStart && p.CreatedAt < monthEnd);
                var lessons = await _context.LessonsLearned.CountAsync(l => l.IsActive && l.CreatedAt >= monthStart && l.CreatedAt < monthEnd);

                vm.MonthlyUploads.Add(new MonthlyUploadItem
                {
                    Month = monthStart.ToString("MMM"),
                    Documents = docs,
                    Policies = policies,
                    Lessons = lessons,
                    Total = docs + policies + lessons
                });
            }

            // ========== CURRENTLY ACTIVE USERS (last activity today) ==========
            vm.CurrentlyActiveUsers = await _context.AuditLogs
                .Where(a => a.IsActive && a.CreatedAt >= today && a.UserId != null)
                .GroupBy(a => new { a.UserId, a.UserEmail, a.UserName })
                .Select(g => new ActiveUserItem
                {
                    UserId = g.Key.UserId ?? 0,
                    UserEmail = g.Key.UserEmail ?? "Unknown",
                    UserName = g.Key.UserName ?? "Unknown",
                    LoginCount = g.Count(), // Actions today
                    LastLogin = g.Max(a => a.CreatedAt).ToString("HH:mm")
                })
                .OrderByDescending(u => u.LoginCount)
                .Take(15)
                .ToListAsync();

            // Get barangay info for active users
            foreach (var user in vm.CurrentlyActiveUsers)
            {
                var userInfo = await _context.BusinessUsers
                    .Where(u => u.Id == user.UserId)
                    .Select(u => new { u.Role, u.BarangayName })
                    .FirstOrDefaultAsync();
                user.Role = userInfo?.Role ?? "Unknown";
                user.BarangayName = userInfo?.BarangayName ?? "N/A";
            }

            return View(vm);
        }

        #endregion

        #region Barangay Dashboard (barangay roles)

        /// <summary>
        /// Barangay-specific dashboard filtered by the user's BarangayId.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "barangay_admin")]
        public async Task<IActionResult> Barangay()
        {
            var barangayId = GetCurrentBarangayId();
            var role = GetCurrentRole();

            var vm = new BarangayDashboardViewModel
            {
                Role = role,
                RoleLabel = GetRoleLabel(),
                BarangayId = barangayId,
                BarangayName = GetCurrentBarangayName(),
                IsViewOnly = IsViewOnly(),
                CanModify = CanModify()
            };

            var currentEmail = User?.Identity?.Name ?? HttpContext.Session.GetString("UserName") ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(currentEmail))
            {
                var normalizedEmail = currentEmail.Trim().ToLowerInvariant();
                vm.HasApprovedPasswordReset = await _context.PasswordResetRequests
                    .AsNoTracking()
                    .AnyAsync(r => r.IsActive && r.Status == "Approved" && r.Email.ToLower() == normalizedEmail);
            }

            // If no BarangayId is set, show empty dashboard with warning
            if (!barangayId.HasValue)
            {
                vm.WarningMessage = "Your account is not assigned to a barangay. Please contact the administrator.";
                return View(vm);
            }

            // Stats filtered by BarangayId
            vm.TotalDocuments = await _context.KnowledgeDocuments
                .CountAsync(d => d.IsActive && d.BarangayId == barangayId);

            vm.TotalPolicies = await _context.Policies
                .CountAsync(p => p.IsActive && p.BarangayId == barangayId);

            vm.TotalBestPractices = await _context.BestPractices
                .CountAsync(bp => bp.IsActive && bp.BarangayId == barangayId);

            vm.TotalLessonsLearned = await _context.LessonsLearned
                .CountAsync(ll => ll.IsActive && ll.BarangayId == barangayId);

            vm.TotalAnnouncements = await _context.Announcements
                .CountAsync(a => a.IsActive && a.BarangayId == barangayId);

            // Pending items (for admin/secretary)
            if (role == "barangay_admin" || role == "barangay_secretary")
            {
                vm.PendingDocuments = await _context.KnowledgeDocuments
                    .CountAsync(d => d.IsActive && d.BarangayId == barangayId && d.Status == "pending");

                vm.PendingPolicies = await _context.Policies
                    .CountAsync(p => p.IsActive && p.BarangayId == barangayId && p.Status == "pending");
            }

            // Subscription status
            var subscription = await _context.BarangaySubscriptions
                .Include(s => s.Plan)
                .Where(s => s.BarangayId == barangayId && s.IsActive)
                .OrderByDescending(s => s.EndDate)
                .FirstOrDefaultAsync();

            if (subscription != null)
            {
                vm.SubscriptionPlan = subscription.Plan?.Name ?? "Unknown Plan";
                vm.SubscriptionStatus = subscription.Status;
                vm.SubscriptionEndDate = subscription.EndDate.ToString("yyyy-MM-dd");
            }

            // Recent activity for this barangay (last 10)
            vm.RecentActivity = await _context.AuditLogs
                .Where(a => a.IsActive)
                .Join(_context.BusinessUsers.Where(u => u.BarangayId == barangayId),
                    log => log.UserId,
                    user => user.Id,
                    (log, user) => log)
                .OrderByDescending(a => a.CreatedAt)
                .Take(10)
                .Select(a => new ActivityItem
                {
                    Timestamp = a.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                    User = a.UserName ?? a.UserEmail ?? "System",
                    Action = a.Action,
                    Module = a.Module,
                    Target = a.TargetName ?? ""
                })
                .ToListAsync();

            // Get current user ID for "My" queries
            var userIdStr = HttpContext.Session.GetString("UserId");
            int.TryParse(userIdStr, out var currentUserId);

            // ========== ROLE-SPECIFIC DATA ==========

            // ADMIN: Team Overview
            if (role == "barangay_admin")
            {
                vm.StaffCount = await _context.BusinessUsers
                    .CountAsync(u => u.IsActive && u.BarangayId == barangayId);

                // Recent logins (users who logged in within last 7 days)
                var sevenDaysAgo = DateTime.Now.AddDays(-7);
                vm.RecentLoginsCount = await _context.AuditLogs
                    .Where(a => a.IsActive && a.Action == "Login" && a.CreatedAt >= sevenDaysAgo)
                    .Join(_context.BusinessUsers.Where(u => u.BarangayId == barangayId),
                        log => log.UserId,
                        user => user.Id,
                        (log, user) => log)
                    .Select(a => a.UserId)
                    .Distinct()
                    .CountAsync();
            }

            // SECRETARY: My Submissions
            if (role == "barangay_secretary")
            {
                vm.MySubmittedDocuments = await _context.KnowledgeDocuments
                    .CountAsync(d => d.IsActive && d.BarangayId == barangayId && d.UploadedById == currentUserId);

                vm.MySubmittedPolicies = await _context.Policies
                    .CountAsync(p => p.IsActive && p.BarangayId == barangayId && p.AuthorId == currentUserId);
            }

            // STAFF: My Contributions
            if (role == "barangay_staff")
            {
                vm.MyDocuments = await _context.KnowledgeDocuments
                    .CountAsync(d => d.IsActive && d.BarangayId == barangayId && d.UploadedById == currentUserId);

                vm.MyBestPractices = await _context.BestPractices
                    .CountAsync(bp => bp.IsActive && bp.BarangayId == barangayId && bp.SubmittedById == currentUserId);

                vm.MyLessonsLearned = await _context.LessonsLearned
                    .CountAsync(ll => ll.IsActive && ll.BarangayId == barangayId && ll.SubmittedById == currentUserId);
            }

            // STAFF & COUNCIL: Recent Announcements (to stay informed)
            if (role == "barangay_staff" || role == "council_member")
            {
                vm.RecentAnnouncements = await _context.Announcements
                    .Include(a => a.Author)
                    .Where(a => a.IsActive && !a.IsArchived && a.BarangayId == barangayId && a.Status == "published")
                    .OrderByDescending(a => a.IsPinned)
                    .ThenByDescending(a => a.CreatedAt)
                    .Take(5)
                    .Select(a => new DashboardAnnouncementItem
                    {
                        Id = a.Id,
                        Title = a.Title,
                        Content = a.Content.Length > 100 ? a.Content.Substring(0, 100) + "..." : a.Content,
                        Priority = a.Priority,
                        PostedBy = a.Author != null ? a.Author.FullName : "Admin",
                        PostedAt = a.CreatedAt.ToString("MMM dd, yyyy"),
                        IsPinned = a.IsPinned
                    })
                    .ToListAsync();
            }

            // COUNCIL: Recently Approved Items (transparency)
            if (role == "council_member")
            {
                var approvedDocs = await _context.KnowledgeDocuments
                    .Include(d => d.ApprovedBy)
                    .Where(d => d.IsActive && d.BarangayId == barangayId && d.Status == "approved" && d.ApprovedAt != null)
                    .OrderByDescending(d => d.ApprovedAt)
                    .Take(5)
                    .Select(d => new ApprovedItem
                    {
                        Id = d.Id,
                        Title = d.Title,
                        Type = "Document",
                        ApprovedBy = d.ApprovedBy != null ? d.ApprovedBy.FullName : "Admin",
                        ApprovedAt = d.ApprovedAt!.Value.ToString("MMM dd, yyyy")
                    })
                    .ToListAsync();

                var approvedPolicies = await _context.Policies
                    .Include(p => p.ApprovedBy)
                    .Where(p => p.IsActive && p.BarangayId == barangayId && p.Status == "approved" && p.ApprovedAt != null)
                    .OrderByDescending(p => p.ApprovedAt)
                    .Take(5)
                    .Select(p => new ApprovedItem
                    {
                        Id = p.Id,
                        Title = p.Title,
                        Type = "Policy",
                        ApprovedBy = p.ApprovedBy != null ? p.ApprovedBy.FullName : "Admin",
                        ApprovedAt = p.ApprovedAt!.Value.ToString("MMM dd, yyyy")
                    })
                    .ToListAsync();

                vm.RecentlyApproved = approvedDocs.Concat(approvedPolicies)
                    .OrderByDescending(x => x.ApprovedAt)
                    .Take(5)
                    .ToList();
            }

            return View(vm);
        }

        #endregion

        #region Redirect Helper

        /// <summary>
        /// Redirects to the appropriate dashboard based on role.
        /// </summary>
        [HttpGet]
        public IActionResult Index()
        {
            var role = GetCurrentRole();

            return role switch
            {
                "super_admin" => RedirectToAction(nameof(System)),
                "barangay_admin" => RedirectToAction(nameof(Barangay)),
                "barangay_secretary" => RedirectToAction(nameof(Barangay)),
                "barangay_staff" => RedirectToAction(nameof(Barangay)),
                "council_member" => RedirectToAction(nameof(Barangay)),
                _ => RedirectToAction("Login", "Home")
            };
        }

        #endregion
    }

    #region ViewModels

    public class SystemDashboardViewModel
    {
        public string Role { get; set; } = "";
        public string RoleLabel { get; set; } = "";

        // System-wide counts
        public int TotalBarangays { get; set; }
        public int ActiveBarangays { get; set; }
        public int TotalUsers { get; set; }
        public int TotalDocuments { get; set; }
        public int TotalPolicies { get; set; }
        public int TotalBestPractices { get; set; }
        public int TotalLessonsLearned { get; set; }
        public int TotalAnnouncements { get; set; }
        public int ActiveSubscriptions { get; set; }
        public int ExpiredSubscriptions { get; set; }
        public int PendingSubscriptions { get; set; }

        // Revenue
        public decimal TotalRevenue { get; set; }
        public decimal MonthlyRevenue { get; set; }

        // Monthly revenue breakdown (for chart)
        public List<MonthlyRevenueItem> MonthlyRevenueData { get; set; } = new();

        // Pending approvals
        public int PendingDocuments { get; set; }
        public int PendingPolicies { get; set; }
        public int PendingPayments { get; set; }

        // Barangay summaries
        public List<BarangaySummaryItem> BarangaySummaries { get; set; } = new();

        // Subscription report
        public List<SubscriptionReportItem> SubscriptionReport { get; set; } = new();

        // Inactive barangays
        public List<InactiveBarangayItem> InactiveBarangays { get; set; } = new();

        // Recent activity
        public List<ActivityItem> RecentActivity { get; set; } = new();
    }

    public class MonthlyRevenueItem
    {
        public string Month { get; set; } = "";
        public decimal Amount { get; set; }
    }

    public class BarangaySummaryItem
    {
        public int BarangayId { get; set; }
        public string BarangayName { get; set; } = "";
        public int TotalUsers { get; set; }
        public int TotalDocuments { get; set; }
        public int TotalPolicies { get; set; }
        public int TotalLessonsLearned { get; set; }
        public int TotalBestPractices { get; set; }
        public int TotalAnnouncements { get; set; }
        public string PlanName { get; set; } = "None";
        public string SubscriptionStatus { get; set; } = "None";
    }

    public class SubscriptionReportItem
    {
        public string BarangayName { get; set; } = "";
        public string PlanName { get; set; } = "";
        public string PaymentStatus { get; set; } = "";
        public string LastPaymentDate { get; set; } = "N/A";
        public string ExpiryDate { get; set; } = "";
        public decimal Amount { get; set; }
    }

    public class InactiveBarangayItem
    {
        public int BarangayId { get; set; }
        public string BarangayName { get; set; } = "";
        public string LastActivityDate { get; set; } = "Never";
        public string Reason { get; set; } = "";
    }

    public class BarangayDashboardViewModel
    {
        public string Role { get; set; } = "";
        public string RoleLabel { get; set; } = "";
        public int? BarangayId { get; set; }
        public string BarangayName { get; set; } = "";
        public bool IsViewOnly { get; set; }
        public bool CanModify { get; set; }
        public string? WarningMessage { get; set; }
        public bool HasApprovedPasswordReset { get; set; }

        // Barangay-specific counts
        public int TotalDocuments { get; set; }
        public int TotalPolicies { get; set; }
        public int TotalBestPractices { get; set; }
        public int TotalLessonsLearned { get; set; }
        public int TotalAnnouncements { get; set; }

        // Pending items (admin/secretary only)
        public int PendingDocuments { get; set; }
        public int PendingPolicies { get; set; }

        // Subscription info
        public string? SubscriptionPlan { get; set; }
        public string? SubscriptionStatus { get; set; }
        public string? SubscriptionEndDate { get; set; }

        // Role-specific: My Contributions (staff)
        public int MyDocuments { get; set; }
        public int MyBestPractices { get; set; }
        public int MyLessonsLearned { get; set; }

        // Role-specific: My Submissions (secretary)
        public int MySubmittedDocuments { get; set; }
        public int MySubmittedPolicies { get; set; }

        // Role-specific: Team Overview (admin only)
        public int StaffCount { get; set; }
        public int RecentLoginsCount { get; set; }

        // Role-specific: Recent Announcements (staff/council)
        public List<DashboardAnnouncementItem> RecentAnnouncements { get; set; } = new();

        // Role-specific: Recently Approved Items (council)
        public List<ApprovedItem> RecentlyApproved { get; set; } = new();

        // Recent activity
        public List<ActivityItem> RecentActivity { get; set; } = new();
    }

    public class DashboardAnnouncementItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public string Priority { get; set; } = "";
        public string PostedBy { get; set; } = "";
        public string PostedAt { get; set; } = "";
        public bool IsPinned { get; set; }
    }

    public class ApprovedItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Type { get; set; } = "";  // Document or Policy
        public string ApprovedBy { get; set; } = "";
        public string ApprovedAt { get; set; } = "";
    }

    public class ActivityItem
    {
        public string Timestamp { get; set; } = "";
        public string User { get; set; } = "";
        public string Action { get; set; } = "";
        public string Module { get; set; } = "";
        public string Target { get; set; } = "";
    }

    #endregion

    #region Security Monitoring ViewModels

    public class SecurityMonitoringViewModel
    {
        public string Role { get; set; } = "";
        public string Filter { get; set; } = "all";
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalRecords { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize);

        // Top Stats (30d)
        public int TotalLoginsLast30Days { get; set; }
        public int FailedLoginsLast30Days { get; set; }
        public int ActiveSessions { get; set; }
        public int SuspiciousActivity { get; set; }

        // Login Activity Log
        public List<LoginActivityItem> LoginActivities { get; set; } = new();

        // Daily Login Trend (last 7 days) with success/fail split
        public List<DailyLoginItem> DailyLoginTrend { get; set; } = new();

        // Logins By Role
        public List<LoginsByRoleItem> LoginsByRole { get; set; } = new();
    }

    public class LoginActivityItem
    {
        public long Id { get; set; }
        public string UserEmail { get; set; } = "";
        public string UserName { get; set; } = "";
        public string Action { get; set; } = "";
        public string IpAddress { get; set; } = "";
        public string Timestamp { get; set; } = "";
        public int? UserId { get; set; }
        public string BarangayName { get; set; } = "";
    }

    public class LoginsByRoleItem
    {
        public string Role { get; set; } = "";
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    public class DailyLoginItem
    {
        public string Date { get; set; } = "";
        public int Count { get; set; }
        public int FailedCount { get; set; }
    }

    public class ActiveUserItem
    {
        public int UserId { get; set; }
        public string UserEmail { get; set; } = "";
        public string UserName { get; set; } = "";
        public string Role { get; set; } = "";
        public string BarangayName { get; set; } = "";
        public int LoginCount { get; set; }
        public string LastLogin { get; set; } = "";
    }

    #endregion

    #region System Analytics ViewModels

    public class SystemAnalyticsViewModel
    {
        public string Role { get; set; } = "";

        // Top Stats
        public int ActiveUsersToday { get; set; }
        public int ActiveBarangays { get; set; }
        public int UploadsThisMonth { get; set; }
        public double MoMGrowth { get; set; }

        // Uploads by Type (totals)
        public int TotalDocuments { get; set; }
        public int TotalPolicies { get; set; }
        public int TotalLessonsLearned { get; set; }
        public int TotalBestPractices { get; set; }

        // Monthly Uploads (last 7 months)
        public List<MonthlyUploadItem> MonthlyUploads { get; set; } = new();

        // Currently Active Users
        public List<ActiveUserItem> CurrentlyActiveUsers { get; set; } = new();
    }

    public class MonthlyUploadItem
    {
        public string Month { get; set; } = "";
        public int Documents { get; set; }
        public int Policies { get; set; }
        public int Lessons { get; set; }
        public int Total { get; set; }
    }

    public class BarangayActivityItem
    {
        public int BarangayId { get; set; }
        public string BarangayName { get; set; } = "";
        public int ActivityCount { get; set; }
        public int DocumentCount { get; set; }
        public int UserCount { get; set; }
    }

    public class RoleDistributionItem
    {
        public string Role { get; set; } = "";
        public int Count { get; set; }
    }

    public class SystemEventItem
    {
        public string Timestamp { get; set; } = "";
        public string User { get; set; } = "";
        public string Action { get; set; } = "";
        public string Module { get; set; } = "";
        public string Target { get; set; } = "";
    }

    #endregion

    #region System Monitoring ViewModels

    public class SystemMonitoringDashboardViewModel
    {
        public string Role { get; set; } = "";

        // Aggregate Stats
        public int TotalBarangays { get; set; }
        public int TotalUsers { get; set; }
        public int TotalDocuments { get; set; }
        public int TotalPolicies { get; set; }
        public int TotalLessonsLearned { get; set; }
        public int TotalBestPractices { get; set; }

        // Growth This Month
        public int NewBarangaysThisMonth { get; set; }
        public int NewUsersThisMonth { get; set; }
        public int NewDocumentsThisMonth { get; set; }
        public int NewPoliciesThisMonth { get; set; }
        public int NewLessonsThisMonth { get; set; }
        public int NewBestPracticesThisMonth { get; set; }

        // Per-Barangay Summary
        public List<PerBarangaySummaryItem> BarangaySummaries { get; set; } = new();
    }

    public class PerBarangaySummaryItem
    {
        public int BarangayId { get; set; }
        public string BarangayName { get; set; } = "";
        public int UserCount { get; set; }
        public int DocumentCount { get; set; }
        public int PolicyCount { get; set; }
        public int LessonCount { get; set; }
        public int BestPracticeCount { get; set; }
        public string SubscriptionStatus { get; set; } = "";
        public string PlanName { get; set; } = "";
    }

    #endregion
}
