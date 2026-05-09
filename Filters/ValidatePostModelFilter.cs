using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace JAS_MINE_IT15.Filters
{
    /// <summary>
    /// Enforces ModelState validation for all POST actions unless explicitly handled.
    /// Returns generic validation messages to avoid leaking internal details.
    /// Logs validation failures for security monitoring.
    /// </summary>
    public class ValidatePostModelFilter : IActionFilter
    {
        private readonly ILogger<ValidatePostModelFilter> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ValidatePostModelFilter(ILogger<ValidatePostModelFilter> logger, IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (!HttpMethods.IsPost(context.HttpContext.Request.Method))
                return;

            if (context.ModelState.IsValid)
                return;

            // Log validation failure for security monitoring
            var userId = context.HttpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "unknown";
            var ipAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var controller = context.RouteData.Values["controller"]?.ToString() ?? "unknown";
            var action = context.RouteData.Values["action"]?.ToString() ?? "unknown";
            
            var errors = string.Join("; ", context.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));

            _logger.LogWarning(
                "Validation failure detected | User: {UserId} | IP: {IpAddress} | Controller: {Controller} | Action: {Action} | Errors: {Errors}",
                userId, ipAddress, controller, action, errors);

            var isApiRequest = context.HttpContext.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase)
                || context.HttpContext.Request.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase)
                || string.Equals(context.HttpContext.Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);

            if (isApiRequest)
            {
                context.Result = new BadRequestObjectResult(new
                {
                    success = false,
                    message = "Validation failed. Please review your input.",
                    timestamp = DateTime.UtcNow
                });
                return;
            }

            if (context.Controller is Controller mvcController)
            {
                mvcController.ViewData["ErrorMessage"] = "Please correct the highlighted fields.";
                var model = context.ActionArguments.Values.FirstOrDefault(v => v != null && !IsSimpleType(v.GetType()));
                context.Result = model is null ? mvcController.View() : mvcController.View(model);
                return;
            }

            context.Result = new BadRequestObjectResult(new
            {
                success = false,
                message = "Validation failed. Please review your input.",
                timestamp = DateTime.UtcNow
            });
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }

        private static bool IsSimpleType(Type type)
        {
            var t = Nullable.GetUnderlyingType(type) ?? type;
            return t.IsPrimitive || t.IsEnum || t == typeof(string) || t == typeof(decimal) || t == typeof(DateTime) || t == typeof(Guid);
        }
    }
}