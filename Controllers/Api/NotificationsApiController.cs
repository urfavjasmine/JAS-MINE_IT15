using JAS_MINE_IT15.Data;
using JAS_MINE_IT15.Models.Entities;
using JAS_MINE_IT15.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace JAS_MINE_IT15.Controllers.Api
{
    /// <summary>
    /// RESTful API for Notification management.
    /// Provides endpoints for retrieving, marking as read, and managing notifications.
    /// </summary>
    [ApiController]
    [Authorize]
    [EnableRateLimiting("api")]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class NotificationsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationsApiController> _logger;

        public NotificationsApiController(
            ApplicationDbContext context,
            INotificationService notificationService,
            ILogger<NotificationsApiController> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
        }

        #region DTO Classes

        public class NotificationDto
        {
            public int Id { get; set; }
            public string Title { get; set; } = "";
            public string Message { get; set; } = "";
            public string Type { get; set; } = "info";
            public string? Link { get; set; }
            public bool IsRead { get; set; }
            public string Time { get; set; } = "";
            public DateTime CreatedAt { get; set; }
        }

        public class NotificationsResponse
        {
            public List<NotificationDto> Notifications { get; set; } = new();
            public int UnreadCount { get; set; }
            public int TotalCount { get; set; }
        }

        public class MarkReadRequest
        {
            public int? NotificationId { get; set; }
            public bool MarkAll { get; set; } = false;
        }

        #endregion

        /// <summary>
        /// Get notifications for the current user.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<NotificationsResponse>> GetNotifications(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] bool unreadOnly = false)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized(new { message = "User not authenticated" });

            var query = _context.Notifications
                .Where(n => n.UserId == userId && n.IsActive)
                .OrderByDescending(n => n.CreatedAt);

            if (unreadOnly)
                query = (IOrderedQueryable<Notification>)query.Where(n => !n.IsRead);

            var totalCount = await query.CountAsync();
            var unreadCount = await _context.Notifications
                .Where(n => n.UserId == userId && n.IsActive && !n.IsRead)
                .CountAsync();

            var notifications = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.Type,
                    Link = n.Link,
                    IsRead = n.IsRead,
                    Time = FormatTimeAgo(n.CreatedAt),
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();

            return Ok(new NotificationsResponse
            {
                Notifications = notifications,
                UnreadCount = unreadCount,
                TotalCount = totalCount
            });
        }

        /// <summary>
        /// Get unread notification count for the current user.
        /// </summary>
        [HttpGet("count")]
        public async Task<ActionResult<object>> GetUnreadCount()
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized(new { message = "User not authenticated" });

            var count = await _notificationService.GetUnreadCount(userId);
            return Ok(new { unreadCount = count });
        }

        /// <summary>
        /// Mark notification(s) as read.
        /// </summary>
        [HttpPost("mark-read")]
        public async Task<ActionResult> MarkAsRead([FromBody] MarkReadRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized(new { message = "User not authenticated" });

            if (request.MarkAll)
            {
                await _notificationService.MarkAllAsRead(userId);
                return Ok(new { message = "All notifications marked as read" });
            }
            else if (request.NotificationId.HasValue)
            {
                await _notificationService.MarkAsRead(request.NotificationId.Value, userId);
                return Ok(new { message = "Notification marked as read" });
            }

            return BadRequest(new { message = "Either NotificationId or MarkAll must be specified" });
        }

        /// <summary>
        /// Delete a notification (soft delete).
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteNotification(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized(new { message = "User not authenticated" });

            var notification = await _context.Notifications
                .Where(n => n.Id == id && n.UserId == userId)
                .FirstOrDefaultAsync();

            if (notification == null)
                return NotFound(new { message = "Notification not found" });

            notification.IsActive = false;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Notification deleted" });
        }

        /// <summary>
        /// Clear all notifications for the current user (soft delete).
        /// </summary>
        [HttpDelete("clear")]
        public async Task<ActionResult> ClearAllNotifications()
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized(new { message = "User not authenticated" });

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && n.IsActive)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsActive = false;
            }
            await _context.SaveChangesAsync();

            return Ok(new { message = "All notifications cleared", count = notifications.Count });
        }

        #region Helper Methods

        private int GetCurrentUserId()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (int.TryParse(userIdStr, out var userId))
                return userId;

            // Fallback: try to get from session int
            return HttpContext.Session.GetInt32("UserId") ?? 0;
        }

        private static string FormatTimeAgo(DateTime dateTime)
        {
            var diff = DateTime.Now - dateTime;

            if (diff.TotalMinutes < 1) return "Just now";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} min ago";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} hour(s) ago";
            if (diff.TotalDays < 7) return $"{(int)diff.TotalDays} day(s) ago";
            return dateTime.ToString("MMM dd");
        }

        #endregion
    }
}
