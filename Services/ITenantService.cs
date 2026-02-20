using System;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace JAS_MINE_IT15.Services
{
    /// <summary>
    /// Interface for tenant-aware filtering service.
    /// Provides multi-tenant barangay filtering across all modules.
    /// </summary>
    public interface ITenantService
    {
        /// <summary>
        /// Gets the current user's BarangayId from session.
        /// Returns null if not set or invalid.
        /// </summary>
        int? GetCurrentBarangayId();

        /// <summary>
        /// Checks if current user is super_admin (system-wide access).
        /// </summary>
        bool IsSuperAdmin();

        /// <summary>
        /// Checks if current user has view-only access (council_member).
        /// </summary>
        bool IsViewOnly();

        /// <summary>
        /// Checks if current user can create/edit/archive.
        /// </summary>
        bool CanModify();

        /// <summary>
        /// Gets the current role from session.
        /// </summary>
        string GetCurrentRole();

        /// <summary>
        /// Gets the current user's email from session.
        /// </summary>
        string GetCurrentUserEmail();

        /// <summary>
        /// Checks if a user is logged in.
        /// </summary>
        bool IsLoggedIn();
    }

    /// <summary>
    /// Implementation of tenant-aware filtering service.
    /// Inject this service into controllers requiring multi-tenant data filtering.
    /// </summary>
    public class TenantService : ITenantService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TenantService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private HttpContext? HttpContext => _httpContextAccessor.HttpContext;

        public int? GetCurrentBarangayId()
        {
            var barangayIdStr = HttpContext?.Session.GetString("BarangayId");
            if (int.TryParse(barangayIdStr, out var id))
                return id;
            return null;
        }

        public bool IsSuperAdmin()
        {
            return GetCurrentRole() == "super_admin";
        }

        public bool IsViewOnly()
        {
            return GetCurrentRole() == "council_member";
        }

        public bool CanModify()
        {
            var role = GetCurrentRole();
            return role == "super_admin" || role == "barangay_admin" || role == "barangay_secretary" || role == "barangay_staff";
        }

        public string GetCurrentRole()
        {
            return HttpContext?.Session.GetString("Role") ?? "";
        }

        public string GetCurrentUserEmail()
        {
            return HttpContext?.Session.GetString("UserName") ?? "";
        }

        public bool IsLoggedIn()
        {
            return !string.IsNullOrEmpty(HttpContext?.Session.GetString("UserName"));
        }
    }
}
