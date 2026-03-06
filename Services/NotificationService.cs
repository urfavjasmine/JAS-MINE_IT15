using JAS_MINE_IT15.Data;
using JAS_MINE_IT15.Hubs;
using JAS_MINE_IT15.Models.Entities;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace JAS_MINE_IT15.Services
{
    /// <summary>
    /// Service for sending real-time notifications via SignalR and persisting to database.
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

        /// <summary>
        /// Notifies all admins in a barangay about a new pending lesson learned.
        /// </summary>
        Task NotifyPendingLesson(int barangayId, string lessonTitle, string submittedBy);

        /// <summary>
        /// Notifies all admins in a barangay about a new pending best practice.
        /// </summary>
        Task NotifyPendingPractice(int barangayId, string practiceTitle, string submittedBy);

        /// <summary>
        /// Notifies all users in a barangay about a new published announcement.
        /// </summary>
        Task NotifyNewAnnouncement(int barangayId, string title, string priority, string authorName);

        /// <summary>
        /// Notifies users about a policy status change.
        /// </summary>
        Task NotifyPolicyStatusChange(int barangayId, string policyTitle, string newStatus);

        /// <summary>
        /// Notifies users about a lesson learned status change.
        /// </summary>
        Task NotifyLessonStatusChange(int barangayId, string lessonTitle, string newStatus);

        /// <summary>
        /// Notifies users about a best practice status change.
        /// </summary>
        Task NotifyPracticeStatusChange(int barangayId, string practiceTitle, string newStatus);

        /// <summary>
        /// Notifies all users in a barangay about a new discussion reply.
        /// </summary>
        Task NotifyDiscussionReply(int barangayId, string discussionTitle, string replierName);

        /// <summary>
        /// Mark a notification as read.
        /// </summary>
        Task MarkAsRead(int notificationId, int userId);

        /// <summary>
        /// Mark all notifications as read for a user.
        /// </summary>
        Task MarkAllAsRead(int userId);

        /// <summary>
        /// Get unread notification count for a user.
        /// </summary>
        Task<int> GetUnreadCount(int userId);
    }

    public class NotificationService : INotificationService
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ApplicationDbContext _context;

        public NotificationService(IHubContext<NotificationHub> hubContext, ApplicationDbContext context)
        {
            _hubContext = hubContext;
            _context = context;
        }

        /// <summary>
        /// Persist notification for admin users in a barangay.
        /// </summary>
        private async Task PersistForAdmins(int barangayId, string title, string message, string type, string link, string? entityType = null, int? entityId = null)
        {
            var adminUsers = await _context.BusinessUsers
                .Where(u => u.BarangayId == barangayId && u.Role == "barangay_admin" && u.IsActive)
                .Select(u => u.Id)
                .ToListAsync();

            foreach (var userId in adminUsers)
            {
                var notification = new Notification
                {
                    UserId = userId,
                    BarangayId = barangayId,
                    Title = title,
                    Message = message,
                    Type = type,
                    Link = link,
                    RelatedEntityType = entityType,
                    RelatedEntityId = entityId,
                    CreatedAt = DateTime.Now
                };
                _context.Notifications.Add(notification);
            }
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Persist notification for all users in a barangay.
        /// </summary>
        private async Task PersistForBarangay(int barangayId, string title, string message, string type, string link, string? entityType = null, int? entityId = null)
        {
            var users = await _context.BusinessUsers
                .Where(u => u.BarangayId == barangayId && u.IsActive)
                .Select(u => u.Id)
                .ToListAsync();

            foreach (var userId in users)
            {
                var notification = new Notification
                {
                    UserId = userId,
                    BarangayId = barangayId,
                    Title = title,
                    Message = message,
                    Type = type,
                    Link = link,
                    RelatedEntityType = entityType,
                    RelatedEntityId = entityId,
                    CreatedAt = DateTime.Now
                };
                _context.Notifications.Add(notification);
            }
            await _context.SaveChangesAsync();
        }

        public async Task NotifyPendingDocument(int barangayId, string documentTitle, string uploadedBy)
        {
            var title = "New document pending approval";
            var message = $"'{documentTitle}' uploaded by {uploadedBy}";
            var type = "pending";
            var link = "/Home/KnowledgeRepository?status=pending&archiveStatus=active";

            // Persist to database
            await PersistForAdmins(barangayId, title, message, type, link, "Document");

            // Send real-time notification
            var notification = new { Title = title, Message = message, Type = type, Link = link, Time = DateTime.Now.ToString("h:mm tt") };
            await _hubContext.Clients.Group($"barangay_{barangayId}_admins").SendAsync("ReceiveNotification", notification);
        }

        public async Task NotifyPendingPolicy(int barangayId, string policyTitle, string uploadedBy)
        {
            var title = "New policy pending approval";
            var message = $"'{policyTitle}' uploaded by {uploadedBy}";
            var type = "pending";
            var link = "/Home/PoliciesManagement?status=pending&archiveStatus=active";

            await PersistForAdmins(barangayId, title, message, type, link, "Policy");

            var notification = new { Title = title, Message = message, Type = type, Link = link, Time = DateTime.Now.ToString("h:mm tt") };
            await _hubContext.Clients.Group($"barangay_{barangayId}_admins").SendAsync("ReceiveNotification", notification);
        }

        public async Task NotifyDocumentStatusChange(int barangayId, string documentTitle, string newStatus)
        {
            var title = $"Document {newStatus}";
            var message = $"'{documentTitle}' has been {newStatus}";
            var type = newStatus == "approved" ? "approval" : "rejected";
            var link = "/Home/KnowledgeRepository";

            await PersistForBarangay(barangayId, title, message, type, link, "Document");

            var notification = new { Title = title, Message = message, Type = type, Link = link, Time = DateTime.Now.ToString("h:mm tt") };
            await _hubContext.Clients.Group($"barangay_{barangayId}").SendAsync("ReceiveNotification", notification);
        }

        public async Task NotifyBarangay(int barangayId, string title, string message, string type)
        {
            var link = "#";

            await PersistForBarangay(barangayId, title, message, type, link);

            var notification = new { Title = title, Message = message, Type = type, Link = link, Time = DateTime.Now.ToString("h:mm tt") };
            await _hubContext.Clients.Group($"barangay_{barangayId}").SendAsync("ReceiveNotification", notification);
        }

        public async Task NotifyPendingLesson(int barangayId, string lessonTitle, string submittedBy)
        {
            var title = "New lesson pending approval";
            var message = $"'{lessonTitle}' submitted by {submittedBy}";
            var type = "pending";
            var link = "/Home/LessonsLearned?archiveStatus=active";

            await PersistForAdmins(barangayId, title, message, type, link, "Lesson");

            var notification = new { Title = title, Message = message, Type = type, Link = link, Time = DateTime.Now.ToString("h:mm tt") };
            await _hubContext.Clients.Group($"barangay_{barangayId}_admins").SendAsync("ReceiveNotification", notification);
        }

        public async Task NotifyPendingPractice(int barangayId, string practiceTitle, string submittedBy)
        {
            var title = "New best practice pending approval";
            var message = $"'{practiceTitle}' submitted by {submittedBy}";
            var type = "pending";
            var link = "/Home/BestPractices?archiveStatus=active";

            await PersistForAdmins(barangayId, title, message, type, link, "Practice");

            var notification = new { Title = title, Message = message, Type = type, Link = link, Time = DateTime.Now.ToString("h:mm tt") };
            await _hubContext.Clients.Group($"barangay_{barangayId}_admins").SendAsync("ReceiveNotification", notification);
        }

        public async Task NotifyNewAnnouncement(int barangayId, string announcementTitle, string priority, string authorName)
        {
            var title = priority == "high" ? "Important Announcement" : "New Announcement";
            var message = $"'{announcementTitle}' posted by {authorName}";
            var type = priority == "high" ? "urgent" : "announcement";
            var link = "/Home/Announcements";

            await PersistForBarangay(barangayId, title, message, type, link, "Announcement");

            var notification = new { Title = title, Message = message, Type = type, Link = link, Time = DateTime.Now.ToString("h:mm tt") };
            await _hubContext.Clients.Group($"barangay_{barangayId}").SendAsync("ReceiveNotification", notification);
        }

        public async Task NotifyPolicyStatusChange(int barangayId, string policyTitle, string newStatus)
        {
            var title = $"Policy {newStatus}";
            var message = $"'{policyTitle}' has been {newStatus}";
            var type = newStatus == "approved" ? "approval" : "rejected";
            var link = "/Home/PoliciesManagement";

            await PersistForBarangay(barangayId, title, message, type, link, "Policy");

            var notification = new { Title = title, Message = message, Type = type, Link = link, Time = DateTime.Now.ToString("h:mm tt") };
            await _hubContext.Clients.Group($"barangay_{barangayId}").SendAsync("ReceiveNotification", notification);
        }

        public async Task NotifyLessonStatusChange(int barangayId, string lessonTitle, string newStatus)
        {
            var title = $"Lesson {newStatus}";
            var message = $"'{lessonTitle}' has been {newStatus}";
            var type = newStatus == "approved" ? "approval" : "rejected";
            var link = "/Home/LessonsLearned";

            await PersistForBarangay(barangayId, title, message, type, link, "Lesson");

            var notification = new { Title = title, Message = message, Type = type, Link = link, Time = DateTime.Now.ToString("h:mm tt") };
            await _hubContext.Clients.Group($"barangay_{barangayId}").SendAsync("ReceiveNotification", notification);
        }

        public async Task NotifyPracticeStatusChange(int barangayId, string practiceTitle, string newStatus)
        {
            var title = $"Best Practice {newStatus}";
            var message = $"'{practiceTitle}' has been {newStatus}";
            var type = newStatus == "approved" ? "approval" : "rejected";
            var link = "/Home/BestPractices";

            await PersistForBarangay(barangayId, title, message, type, link, "Practice");

            var notification = new { Title = title, Message = message, Type = type, Link = link, Time = DateTime.Now.ToString("h:mm tt") };
            await _hubContext.Clients.Group($"barangay_{barangayId}").SendAsync("ReceiveNotification", notification);
        }

        public async Task NotifyDiscussionReply(int barangayId, string discussionTitle, string replierName)
        {
            var title = "New discussion reply";
            var message = $"{replierName} replied to '{discussionTitle}'";
            var type = "discussion";
            var link = "/Home/KnowledgeSharing";

            await PersistForBarangay(barangayId, title, message, type, link, "Discussion");

            var notification = new { Title = title, Message = message, Type = type, Link = link, Time = DateTime.Now.ToString("h:mm tt") };
            await _hubContext.Clients.Group($"barangay_{barangayId}").SendAsync("ReceiveNotification", notification);
        }

        public async Task MarkAsRead(int notificationId, int userId)
        {
            var notification = await _context.Notifications
                .Where(n => n.Id == notificationId && n.UserId == userId && n.IsActive)
                .FirstOrDefaultAsync();

            if (notification != null)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsRead(int userId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead && n.IsActive)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.Now;
            }
            await _context.SaveChangesAsync();
        }

        public async Task<int> GetUnreadCount(int userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead && n.IsActive)
                .CountAsync();
        }
    }
}
