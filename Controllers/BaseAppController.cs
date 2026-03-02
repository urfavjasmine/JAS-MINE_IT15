using JAS_MINE_IT15.Data;
using JAS_MINE_IT15.Models.Entities;
using Microsoft.AspNetCore.Mvc;

namespace JAS_MINE_IT15.Controllers
{
    /// <summary>
    /// Shared base for all authenticated controllers.
    /// Provides session helpers, audit logging, and common utilities.
    /// </summary>
    public abstract class BaseAppController : Controller
    {
        protected readonly ApplicationDbContext _context;

        protected BaseAppController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ── Session helpers ──

        protected bool IsLoggedIn() =>
            !string.IsNullOrEmpty(HttpContext.Session.GetString("UserName"));

        protected bool IsAdminRole()
        {
            var role = HttpContext.Session.GetString("Role");
            return role == "super_admin" || role == "barangay_admin";
        }

        protected string GetCurrentRole() =>
            HttpContext.Session.GetString("Role") ?? "";

        protected int? GetCurrentBarangayId()
        {
            var s = HttpContext.Session.GetString("BarangayId");
            return int.TryParse(s, out var id) ? id : null;
        }

        protected bool IsSuperAdmin() => GetCurrentRole() == "super_admin";

        protected int? GetCurrentUserId()
        {
            var s = HttpContext.Session.GetString("UserId");
            return int.TryParse(s, out var id) ? id : null;
        }

        protected bool IsViewOnly() => GetCurrentRole() == "council_member";

        protected bool CanModify()
        {
            var role = GetCurrentRole();
            return role is "barangay_admin" or "barangay_secretary" or "barangay_staff";
        }

        protected bool IsBarangayRole()
        {
            var role = GetCurrentRole();
            return role is "barangay_admin" or "barangay_secretary" or "barangay_staff" or "council_member";
        }

        protected IActionResult RedirectToDashboard()
        {
            if (GetCurrentRole() == "super_admin")
                return RedirectToAction("System", "Dashboard");
            return RedirectToAction("Barangay", "Dashboard");
        }

        // ── Audit helper ──

        protected async Task LogAuditAsync(string action, string module,
            int? targetId = null, string? targetType = null,
            string? targetName = null, string? description = null)
        {
            try
            {
                var log = new AuditLog
                {
                    UserId = GetCurrentUserId(),
                    UserEmail = HttpContext.Session.GetString("UserName"),
                    UserName = HttpContext.Session.GetString("UserName"),
                    Action = action,
                    Module = module,
                    TargetId = targetId,
                    TargetType = targetType,
                    TargetName = targetName,
                    Description = description,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                    SessionId = HttpContext.Session.Id,
                    BarangayId = GetCurrentBarangayId(),
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };
                _context.AuditLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch { /* audit failure should not crash the request */ }
        }

        protected static string ComputeStatus(string endDate)
        {
            if (DateTime.TryParse(endDate, out var end))
                return end.Date >= DateTime.Today ? "Active" : "Expired";
            return "Expired";
        }
    }
}
