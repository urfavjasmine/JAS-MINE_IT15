using JAS_MINE_IT15.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace JAS_MINE_IT15.Services
{
    /// <summary>
    /// Service for sending real-time notifications via SignalR.
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Notifies all admins in a barangay about a new pending document.
        /// </summary>
        Task NotifyPendingDocument(int barangayId, string documentTitle, string uploadedBy);

        /// <summary>
        /// Notifies all admins in a barangay about a new pending policy.
        /// </summary>
        Task NotifyPendingPolicy(int barangayId, string policyTitle, string uploadedBy);

        /// <summary>
        /// Notifies a user that their document was approved or rejected.
        /// </summary>
        Task NotifyDocumentStatusChange(int barangayId, string documentTitle, string newStatus);

        /// <summary>
        /// Sends a general notification to all users in a barangay.
        /// </summary>
        Task NotifyBarangay(int barangayId, string title, string message, string type);
    }

    public class NotificationService : INotificationService
    {
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task NotifyPendingDocument(int barangayId, string documentTitle, string uploadedBy)
        {
            var notification = new
            {
                Title = $"New document pending approval",
                Message = $"'{documentTitle}' uploaded by {uploadedBy}",
                Type = "pending",
                Link = "/Home/KnowledgeRepository?status=pending&archiveStatus=active",
                Time = DateTime.Now.ToString("h:mm tt")
            };

            // Send to barangay admins only
            await _hubContext.Clients.Group($"barangay_{barangayId}_admins")
                .SendAsync("ReceiveNotification", notification);
        }

        public async Task NotifyPendingPolicy(int barangayId, string policyTitle, string uploadedBy)
        {
            var notification = new
            {
                Title = $"New policy pending approval",
                Message = $"'{policyTitle}' uploaded by {uploadedBy}",
                Type = "pending",
                Link = "/Home/PoliciesManagement?status=pending&archiveStatus=active",
                Time = DateTime.Now.ToString("h:mm tt")
            };

            await _hubContext.Clients.Group($"barangay_{barangayId}_admins")
                .SendAsync("ReceiveNotification", notification);
        }

        public async Task NotifyDocumentStatusChange(int barangayId, string documentTitle, string newStatus)
        {
            var notification = new
            {
                Title = $"Document {newStatus}",
                Message = $"'{documentTitle}' has been {newStatus}",
                Type = newStatus == "approved" ? "approval" : "rejected",
                Link = "/Home/KnowledgeRepository",
                Time = DateTime.Now.ToString("h:mm tt")
            };

            // Notify all users in barangay
            await _hubContext.Clients.Group($"barangay_{barangayId}")
                .SendAsync("ReceiveNotification", notification);
        }

        public async Task NotifyBarangay(int barangayId, string title, string message, string type)
        {
            var notification = new
            {
                Title = title,
                Message = message,
                Type = type,
                Link = "#",
                Time = DateTime.Now.ToString("h:mm tt")
            };

            await _hubContext.Clients.Group($"barangay_{barangayId}")
                .SendAsync("ReceiveNotification", notification);
        }
    }
}
