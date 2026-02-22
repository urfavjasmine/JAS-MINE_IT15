using Microsoft.AspNetCore.SignalR;

namespace JAS_MINE_IT15.Hubs
{
    /// <summary>
    /// SignalR hub for real-time notifications.
    /// Supports per-barangay notification groups.
    /// </summary>
    public class NotificationHub : Hub
    {
        /// <summary>
        /// Called when a client connects. Joins the user to their barangay group.
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var barangayId = httpContext?.Session.GetString("BarangayId");
            var role = httpContext?.Session.GetString("Role");

            // Join barangay-specific group
            if (!string.IsNullOrEmpty(barangayId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"barangay_{barangayId}");
            }

            // Admins also join the admin group for their barangay
            if (role == "barangay_admin" && !string.IsNullOrEmpty(barangayId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"barangay_{barangayId}_admins");
            }

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Called when a client disconnects.
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}
