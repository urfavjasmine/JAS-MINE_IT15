using JAS_MINE_IT15.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace JAS_MINE_IT15.Filters
{
    /// <summary>
    /// Attribute to prevent council_member role from accessing create/edit/delete actions.
    /// Council members have view-only access.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class DenyViewOnlyAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var role = context.HttpContext.Session.GetString("Role") ?? "";

            if (role == "council_member")
            {
                // Redirect to dashboard with error message
                context.HttpContext.Session.SetString("ErrorMessage", "You do not have permission to perform this action.");
                context.Result = new RedirectToActionResult("Barangay", "Dashboard", null);
                return;
            }

            base.OnActionExecuting(context);
        }
    }

    /// <summary>
    /// Attribute to restrict access to specific roles only.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class RequireRolesAttribute : ActionFilterAttribute
    {
        private readonly string[] _allowedRoles;

        public RequireRolesAttribute(params string[] allowedRoles)
        {
            _allowedRoles = allowedRoles;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var role = context.HttpContext.Session.GetString("Role") ?? "";

            if (!_allowedRoles.Contains(role))
            {
                context.HttpContext.Session.SetString("ErrorMessage", "You do not have permission to access this page.");
                context.Result = new RedirectToActionResult("Index", "Dashboard", null);
                return;
            }

            base.OnActionExecuting(context);
        }
    }

    /// <summary>
    /// Blocks create/edit/upload actions when the user's barangay subscription is not Active.
    /// super_admin bypasses the gate.
    /// Apply to controller or individual actions that require an active subscription.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class RequireActiveSubscriptionAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var role = context.HttpContext.Session.GetString("Role") ?? "";

            // Super admin always bypasses
            if (role == "super_admin")
            {
                base.OnActionExecuting(context);
                return;
            }

            var barangayIdStr = context.HttpContext.Session.GetString("BarangayId");
            if (!int.TryParse(barangayIdStr, out var barangayId))
            {
                context.HttpContext.Session.SetString("ErrorMessage", "Your account is not assigned to a barangay.");
                context.Result = new RedirectToActionResult("Barangay", "Dashboard", null);
                return;
            }

            // Check subscription in DB
            var dbContext = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
            var hasActive = dbContext.BarangaySubscriptions
                .Any(s => s.BarangayId == barangayId
                       && s.IsActive
                       && s.Status == "Active"
                       && s.EndDate >= DateTime.Today);

            if (!hasActive)
            {
                context.HttpContext.Items["SubscriptionExpired"] = true;
                context.Result = new RedirectToActionResult("MySubscription", "Home", new { expired = true });
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}
