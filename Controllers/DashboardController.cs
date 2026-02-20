using JAS_MINE_IT15.Data;
using JAS_MINE_IT15.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
            return HttpContext.Session.GetString("Role") ?? "";
        }

        /// <summary>
        /// Gets the current user's BarangayId from session.
        /// </summary>
        private int? GetCurrentBarangayId()
        {
            var barangayIdStr = HttpContext.Session.GetString("BarangayId");
            if (int.TryParse(barangayIdStr, out var id))
                return id;
            return null;
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
                RoleLabel = HttpContext.Session.GetString("RoleLabel") ?? "Super Admin"
            };

            // System-wide stats (no barangay filter)
            vm.TotalBarangays = await _context.Barangays.CountAsync(b => b.IsActive);
            vm.TotalUsers = await _context.BusinessUsers.CountAsync(u => u.IsActive);
            vm.TotalDocuments = await _context.KnowledgeDocuments.CountAsync(d => d.IsActive);
            vm.TotalPolicies = await _context.Policies.CountAsync(p => p.IsActive);
            vm.TotalBestPractices = await _context.BestPractices.CountAsync(bp => bp.IsActive);
            vm.TotalLessonsLearned = await _context.LessonsLearned.CountAsync(ll => ll.IsActive);
            vm.TotalAnnouncements = await _context.Announcements.CountAsync(a => a.IsActive);

            // Active subscriptions
            vm.ActiveSubscriptions = await _context.BarangaySubscriptions
                .CountAsync(s => s.IsActive && s.Status == "Active");

            // Pending approvals
            vm.PendingDocuments = await _context.KnowledgeDocuments
                .CountAsync(d => d.IsActive && d.Status == "pending");
            vm.PendingPolicies = await _context.Policies
                .CountAsync(p => p.IsActive && p.Status == "pending");

            // Recent activity (last 10)
            vm.RecentActivity = await _context.AuditLogs
                .Where(a => a.IsActive)
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

            return View(vm);
        }

        #endregion

        #region Barangay Dashboard (barangay roles)

        /// <summary>
        /// Barangay-specific dashboard filtered by the user's BarangayId.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "barangay_admin,barangay_secretary,barangay_staff,council_member")]
        public async Task<IActionResult> Barangay()
        {
            var barangayId = GetCurrentBarangayId();
            var role = GetCurrentRole();

            var vm = new BarangayDashboardViewModel
            {
                Role = role,
                RoleLabel = HttpContext.Session.GetString("RoleLabel") ?? "",
                BarangayId = barangayId,
                BarangayName = HttpContext.Session.GetString("Barangay") ?? "",
                IsViewOnly = IsViewOnly(),
                CanModify = CanModify()
            };

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
        public int TotalUsers { get; set; }
        public int TotalDocuments { get; set; }
        public int TotalPolicies { get; set; }
        public int TotalBestPractices { get; set; }
        public int TotalLessonsLearned { get; set; }
        public int TotalAnnouncements { get; set; }
        public int ActiveSubscriptions { get; set; }

        // Pending approvals
        public int PendingDocuments { get; set; }
        public int PendingPolicies { get; set; }

        // Recent activity
        public List<ActivityItem> RecentActivity { get; set; } = new();
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

        // Barangay-specific counts
        public int TotalDocuments { get; set; }
        public int TotalPolicies { get; set; }
        public int TotalBestPractices { get; set; }
        public int TotalLessonsLearned { get; set; }
        public int TotalAnnouncements { get; set; }

        // Pending items
        public int PendingDocuments { get; set; }
        public int PendingPolicies { get; set; }

        // Subscription info
        public string? SubscriptionPlan { get; set; }
        public string? SubscriptionStatus { get; set; }
        public string? SubscriptionEndDate { get; set; }

        // Recent activity
        public List<ActivityItem> RecentActivity { get; set; } = new();
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
}
