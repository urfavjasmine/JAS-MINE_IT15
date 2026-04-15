using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace JAS_MINE_IT15.Filters
{
    /// <summary>
    /// Enforces ModelState validation for all POST actions unless explicitly handled.
    /// Returns generic validation messages to avoid leaking internal details.
    /// </summary>
    public class ValidatePostModelFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (!HttpMethods.IsPost(context.HttpContext.Request.Method))
                return;

            if (context.ModelState.IsValid)
                return;

            var isApiRequest = context.HttpContext.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase)
                || context.HttpContext.Request.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase)
                || string.Equals(context.HttpContext.Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);

            if (isApiRequest)
            {
                context.Result = new BadRequestObjectResult(new
                {
                    message = "Validation failed. Please review your input."
                });
                return;
            }

            if (context.Controller is Controller controller)
            {
                controller.ViewData["ErrorMessage"] = "Please correct the highlighted fields.";
                var model = context.ActionArguments.Values.FirstOrDefault(v => v != null && !IsSimpleType(v.GetType()));
                context.Result = model is null ? controller.View() : controller.View(model);
                return;
            }

            context.Result = new BadRequestObjectResult(new
            {
                message = "Validation failed. Please review your input."
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