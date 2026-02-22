using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JAS_MINE_IT15.Data;

namespace JAS_MINE_IT15.ViewComponents
{
    public class NotificationsViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public NotificationsViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var barangayId = HttpContext.Session.GetInt32("BarangayId");
            var role = HttpContext.Session.GetString("Role") ?? "";

            var notifications = new List<NotificationItem>();

            // 1. Recent announcements (last 7 days, published)
            var recentAnnouncements = await _context.Announcements
                .Where(a => a.IsActive && a.Status == "published")
                .Where(a => a.BarangayId == barangayId || a.BarangayId == null) // Barangay-specific or global
                .Where(a => a.PublishedAt != null && a.PublishedAt > DateTime.UtcNow.AddDays(-7))
                .OrderByDescending(a => a.PublishedAt)
                .Take(3)
                .Select(a => new NotificationItem
                {
                    Id = a.Id,
                    Title = a.Title,
                    Time = FormatTimeAgo(a.PublishedAt ?? DateTime.UtcNow),
                    Type = "announcement",
                    Unread = a.PublishedAt > DateTime.UtcNow.AddDays(-1), // Unread if within 24 hours
                    Link = "/Home/Announcements"
                })
                .ToListAsync();

            notifications.AddRange(recentAnnouncements);

            // 2. Pending documents for approval (admin only)
            if (role == "barangay_admin")
            {
                var pendingDocsCount = await _context.KnowledgeDocuments
                    .Where(d => d.IsActive && !d.IsArchived && d.Status == "pending" && d.BarangayId == barangayId)
                    .CountAsync();

                if (pendingDocsCount > 0)
                {
                    notifications.Insert(0, new NotificationItem
                    {
                        Id = 0,
                        Title = $"{pendingDocsCount} document(s) pending approval",
                        Time = "Action required",
                        Type = "pending",
                        Unread = true,
                        Link = "/Home/KnowledgeRepository?status=pending&archiveStatus=active"
                    });
                }

                var pendingPoliciesCount = await _context.Policies
                    .Where(p => p.IsActive && !p.IsArchived && p.Status == "pending" && p.BarangayId == barangayId)
                    .CountAsync();

                if (pendingPoliciesCount > 0)
                {
                    notifications.Insert(0, new NotificationItem
                    {
                        Id = 0,
                        Title = $"{pendingPoliciesCount} policy/policies pending approval",
                        Time = "Action required",
                        Type = "pending",
                        Unread = true,
                        Link = "/Home/PoliciesManagement?status=pending&archiveStatus=active"
                    });
                }
            }

            // 3. Recent uploads by user (for all users)
            var myRecentUploads = await _context.KnowledgeDocuments
                .Where(d => d.IsActive && d.UploadedById == userId && d.Status == "approved")
                .Where(d => d.UpdatedAt != null && d.UpdatedAt > DateTime.UtcNow.AddDays(-7))
                .OrderByDescending(d => d.UpdatedAt)
                .Take(2)
                .Select(d => new NotificationItem
                {
                    Id = d.Id,
                    Title = $"Your document '{TruncateTitle(d.Title)}' was approved",
                    Time = FormatTimeAgo(d.UpdatedAt ?? DateTime.UtcNow),
                    Type = "approval",
                    Unread = d.UpdatedAt > DateTime.UtcNow.AddDays(-1),
                    Link = "/Home/KnowledgeRepository"
                })
                .ToListAsync();

            notifications.AddRange(myRecentUploads);

            // Limit to 5 notifications max
            var model = new NotificationsViewModel
            {
                Notifications = notifications.Take(5).ToList(),
                UnreadCount = notifications.Count(n => n.Unread)
            };

            return View(model);
        }

        private static string FormatTimeAgo(DateTime dateTime)
        {
            var diff = DateTime.UtcNow - dateTime;

            if (diff.TotalMinutes < 1) return "Just now";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} min ago";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} hour(s) ago";
            if (diff.TotalDays < 7) return $"{(int)diff.TotalDays} day(s) ago";
            return dateTime.ToString("MMM dd");
        }

        private static string TruncateTitle(string title)
        {
            if (string.IsNullOrEmpty(title)) return "";
            return title.Length > 25 ? title.Substring(0, 25) + "..." : title;
        }
    }

    public class NotificationItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Time { get; set; } = "";
        public string Type { get; set; } = "info"; // announcement, pending, approval
        public bool Unread { get; set; }
        public string Link { get; set; } = "#";
    }

    public class NotificationsViewModel
    {
        public List<NotificationItem> Notifications { get; set; } = new();
        public int UnreadCount { get; set; }
    }
}
