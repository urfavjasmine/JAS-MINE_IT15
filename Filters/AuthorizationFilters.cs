using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

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
}
