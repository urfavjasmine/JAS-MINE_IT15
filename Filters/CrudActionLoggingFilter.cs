using Microsoft.AspNetCore.Mvc.Filters;
using JAS_MINE_IT15.Services;

namespace JAS_MINE_IT15.Filters
{
    /// <summary>
    /// Logs important write actions (POST/PUT/DELETE) without recording sensitive input payloads.
    /// </summary>
    public class CrudActionLoggingFilter : IAsyncActionFilter
    {
        private readonly ILogger<CrudActionLoggingFilter> _logger;
        private readonly IAuditService _auditService;

        public CrudActionLoggingFilter(ILogger<CrudActionLoggingFilter> logger, IAuditService auditService)
        {
            _logger = logger;
            _auditService = auditService;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var method = context.HttpContext.Request.Method;
            var shouldLog = HttpMethods.IsPost(method) || HttpMethods.IsPut(method) || HttpMethods.IsDelete(method);

            if (!shouldLog)
            {
                await next();
                return;
            }

            var executed = await next();

            var path = context.HttpContext.Request.Path.Value ?? string.Empty;
            var action = context.ActionDescriptor.RouteValues.TryGetValue("action", out var actionName) ? actionName : "unknown";
            var controller = context.ActionDescriptor.RouteValues.TryGetValue("controller", out var controllerName) ? controllerName : "unknown";
            var user = context.HttpContext.User?.Identity?.Name ?? "anonymous";
            var statusCode = context.HttpContext.Response.StatusCode;

            if (executed.Exception is null)
            {
                _logger.LogInformation(
                    "System action: {Method} {Controller}/{Action} path={Path} user={User} status={StatusCode}",
                    method,
                    controller,
                    action,
                    path,
                    user,
                    statusCode);

                await _auditService.LogAsync(
                    action: method,
                    module: $"{controller}Controller",
                    targetId: null,
                    targetType: "HttpAction",
                    targetName: $"{controller}/{action}",
                    description: $"HTTP {method} {path} completed with status {statusCode}");
            }
            else
            {
                _logger.LogWarning(
                    executed.Exception,
                    "System action failed: {Method} {Controller}/{Action} path={Path} user={User}",
                    method,
                    controller,
                    action,
                    path,
                    user);

                await _auditService.LogAsync(
                    action: $"{method}_FAILED",
                    module: $"{controller}Controller",
                    targetId: null,
                    targetType: "HttpAction",
                    targetName: $"{controller}/{action}",
                    description: $"HTTP {method} {path} failed: {executed.Exception.Message}");
            }
        }
    }
}