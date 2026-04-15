using Microsoft.AspNetCore.Mvc.Filters;
using System.Reflection;
using System.Text.RegularExpressions;

namespace JAS_MINE_IT15.Filters
{
    /// <summary>
    /// Trims and normalizes incoming string values to reduce input abuse surface.
    /// Output encoding should still be handled in Razor/UI rendering.
    /// </summary>
    public class SanitizeInputFilter : IActionFilter
    {
        private static readonly Regex ControlChars = new("[\u0000-\u001F\u007F]", RegexOptions.Compiled);

        public void OnActionExecuting(ActionExecutingContext context)
        {
            foreach (var key in context.ActionArguments.Keys.ToList())
            {
                context.ActionArguments[key] = SanitizeValue(context.ActionArguments[key]);
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }

        private static object? SanitizeValue(object? value)
        {
            if (value is null) return null;

            if (value is string s)
            {
                var trimmed = s.Trim();
                return ControlChars.Replace(trimmed, string.Empty);
            }

            var type = value.GetType();
            if (type.IsPrimitive || type.IsEnum || type == typeof(DateTime) || type == typeof(decimal))
                return value;

            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!prop.CanRead || !prop.CanWrite) continue;
                if (prop.PropertyType != typeof(string)) continue;

                var current = prop.GetValue(value) as string;
                if (current is null) continue;

                var sanitized = ControlChars.Replace(current.Trim(), string.Empty);
                prop.SetValue(value, sanitized);
            }

            return value;
        }
    }
}
