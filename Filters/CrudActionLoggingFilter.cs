using Microsoft.AspNetCore.Mvc.Filters;

namespace JAS_MINE_IT15.Filters
{
    /// <summary>
    /// Logs important write actions (POST/PUT/DELETE) without recording sensitive input payloads.
    /// </summary>
    public class CrudActionLoggingFilter : IAsyncActionFilter
    {
        private readonly ILogger<CrudActionLoggingFilter> _logger;

        public CrudActionLoggingFilter(ILogger<CrudActionLoggingFilter> logger)
        {
            _logger = logger;
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
            }
        }
    }
}