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

            // 1. Get persisted notifications from database (user-specific, last 7 days)
            if (userId > 0)
            {
                var persistedNotifications = await _context.Notifications
                    .Where(n => n.UserId == userId && n.IsActive)
                    .Where(n => n.CreatedAt > DateTime.Now.AddDays(-7))
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(10)
                    .Select(n => new NotificationItem
                    {
                        Id = n.Id,
                        Title = n.Title,
                        Time = FormatTimeAgo(n.CreatedAt),
                        Type = n.Type,
                        Unread = !n.IsRead,
                        Link = n.Link ?? "#",
                        IsPersisted = true
                    })
                    .ToListAsync();

                notifications.AddRange(persistedNotifications);
            }

            // 2. Add computed pending counts for admins (real-time data)
            if (role == "barangay_admin")
            {
                var pendingDocsCount = await _context.KnowledgeDocuments
                    .Where(d => d.IsActive && !d.IsArchived && d.Status == "pending" && d.BarangayId == barangayId)
                    .CountAsync();

                if (pendingDocsCount > 0 && !notifications.Any(n => n.Title.Contains("document") && n.Title.Contains("pending")))
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

                if (pendingPoliciesCount > 0 && !notifications.Any(n => n.Title.Contains("policy") && n.Title.Contains("pending")))
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

                var pendingLessonsCount = await _context.LessonsLearned
                    .Where(l => !l.IsArchived && l.Status == "pending" && l.BarangayId == barangayId)
                    .CountAsync();

                if (pendingLessonsCount > 0 && !notifications.Any(n => n.Title.Contains("lesson") && n.Title.Contains("pending")))
                {
                    notifications.Insert(0, new NotificationItem
                    {
                        Id = 0,
                        Title = $"{pendingLessonsCount} lesson(s) pending approval",
                        Time = "Action required",
                        Type = "pending",
                        Unread = true,
                        Link = "/Home/LessonsLearned?archiveStatus=active"
                    });
                }

                var pendingPracticesCount = await _context.BestPractices
                    .Where(bp => !bp.IsArchived && bp.Status == "pending" && bp.BarangayId == barangayId)
                    .CountAsync();

                if (pendingPracticesCount > 0 && !notifications.Any(n => n.Title.Contains("practice") && n.Title.Contains("pending")))
                {
                    notifications.Insert(0, new NotificationItem
                    {
                        Id = 0,
                        Title = $"{pendingPracticesCount} practice(s) pending approval",
                        Time = "Action required",
                        Type = "pending",
                        Unread = true,
                        Link = "/Home/BestPractices?archiveStatus=active"
                    });
                }

                // --- BARANGAY ADMIN: Subscription/Payment Notifications ---
                // Check for pending invoices (unpaid)
                if (barangayId.HasValue)
                {
                    var unpaidInvoices = await _context.Invoices
                        .Where(i => i.IsActive && i.BarangayId == barangayId && i.Status == "Unpaid")
                        .CountAsync();

                    if (unpaidInvoices > 0)
                    {
                        notifications.Insert(0, new NotificationItem
                        {
                            Id = 0,
                            Title = $"{unpaidInvoices} unpaid invoice(s)",
                            Time = "Payment required",
                            Type = "urgent",
                            Unread = true,
                            Link = "/Home/MySubscription"
                        });
                    }

                    // Check for overdue invoices
                    var overdueInvoices = await _context.Invoices
                        .Where(i => i.IsActive && i.BarangayId == barangayId && i.Status == "Unpaid" && i.DueDate < DateTime.Today)
                        .CountAsync();

                    if (overdueInvoices > 0)
                    {
                        notifications.Insert(0, new NotificationItem
                        {
                            Id = 0,
                            Title = $"{overdueInvoices} overdue invoice(s)!",
                            Time = "Urgent",
                            Type = "urgent",
                            Unread = true,
                            Link = "/Home/MySubscription"
                        });
                    }

                    // Check for subscription expiring soon (within 7 days)
                    var expiringSoon = await _context.BarangaySubscriptions
                        .Where(s => s.IsActive && s.BarangayId == barangayId && s.Status == "Active")
                        .Where(s => s.EndDate <= DateTime.Today.AddDays(7) && s.EndDate >= DateTime.Today)
                        .CountAsync();

                    if (expiringSoon > 0)
                    {
                        notifications.Insert(0, new NotificationItem
                        {
                            Id = 0,
                            Title = "Subscription expiring soon!",
                            Time = "Within 7 days",
                            Type = "urgent",
                            Unread = true,
                            Link = "/Home/MySubscription"
                        });
                    }
                }
            }

            // --- SUPER ADMIN: Payment Verification and System Notifications ---
            if (role == "super_admin")
            {
                // Pending payment verifications (proofs uploaded, waiting for approval)
                var pendingVerifications = await _context.SubscriptionPayments
                    .Where(p => p.IsActive && p.Status == "PendingVerification")
                    .CountAsync();

                if (pendingVerifications > 0)
                {
                    notifications.Insert(0, new NotificationItem
                    {
                        Id = 0,
                        Title = $"{pendingVerifications} payment(s) awaiting verification",
                        Time = "Action required",
                        Type = "pending",
                        Unread = true,
                        Link = "/Home/SubscriptionPayments"
                    });
                }

                // Overdue invoices across all barangays
                var overdueInvoicesAll = await _context.Invoices
                    .Where(i => i.IsActive && i.Status == "Unpaid" && i.DueDate < DateTime.Today)
                    .CountAsync();

                if (overdueInvoicesAll > 0)
                {
                    notifications.Insert(0, new NotificationItem
                    {
                        Id = 0,
                        Title = $"{overdueInvoicesAll} overdue invoice(s) system-wide",
                        Time = "Follow up needed",
                        Type = "urgent",
                        Unread = true,
                        Link = "/Home/BarangaySubscriptions"
                    });
                }

                // Pending subscriptions (subscriptions awaiting payment)
                var pendingSubscriptions = await _context.BarangaySubscriptions
                    .Where(s => s.IsActive && s.Status == "Pending")
                    .CountAsync();

                if (pendingSubscriptions > 0)
                {
                    notifications.Insert(0, new NotificationItem
                    {
                        Id = 0,
                        Title = $"{pendingSubscriptions} pending subscription(s)",
                        Time = "Awaiting payment",
                        Type = "pending",
                        Unread = true,
                        Link = "/Home/BarangaySubscriptions"
                    });
                }

                // Subscriptions expiring within 7 days
                var expiringSoonAll = await _context.BarangaySubscriptions
                    .Where(s => s.IsActive && s.Status == "Active")
                    .Where(s => s.EndDate <= DateTime.Today.AddDays(7) && s.EndDate >= DateTime.Today)
                    .CountAsync();

                if (expiringSoonAll > 0)
                {
                    notifications.Insert(0, new NotificationItem
                    {
                        Id = 0,
                        Title = $"{expiringSoonAll} subscription(s) expiring soon",
                        Time = "Within 7 days",
                        Type = "urgent",
                        Unread = true,
                        Link = "/Home/BarangaySubscriptions"
                    });
                }

                // New barangay registrations in last 24 hours
                var newBarangays = await _context.Barangays
                    .Where(b => b.IsActive && b.CreatedAt >= DateTime.Now.AddHours(-24))
                    .CountAsync();

                if (newBarangays > 0)
                {
                    notifications.Insert(0, new NotificationItem
                    {
                        Id = 0,
                        Title = $"{newBarangays} new barangay registration(s)",
                        Time = "Last 24 hours",
                        Type = "info",
                        Unread = true,
                        Link = "/Home/BarangaysManagement"
                    });
                }
            }

            // 3. Fallback: Recent announcements if no persisted notifications
            if (notifications.Count < 3)
            {
                var recentAnnouncements = await _context.Announcements
                    .Where(a => a.IsActive && a.Status == "published")
                    .Where(a => a.BarangayId == barangayId || a.BarangayId == null)
                    .Where(a => a.PublishedAt != null && a.PublishedAt > DateTime.UtcNow.AddDays(-7))
                    .OrderByDescending(a => a.PublishedAt)
                    .Take(3)
                    .Select(a => new NotificationItem
                    {
                        Id = a.Id,
                        Title = a.Title,
                        Time = FormatTimeAgo(a.PublishedAt ?? DateTime.UtcNow),
                        Type = "announcement",
                        Unread = a.PublishedAt > DateTime.UtcNow.AddDays(-1),
                        Link = "/Home/Announcements"
                    })
                    .ToListAsync();

                // Only add announcements that aren't already in persisted notifications
                foreach (var ann in recentAnnouncements)
                {
                    if (!notifications.Any(n => n.Title == ann.Title && n.Type == "announcement"))
                    {
                        notifications.Add(ann);
                    }
                }
            }

            // Limit to 8 notifications max, prioritize unread
            var sortedNotifications = notifications
                .OrderByDescending(n => n.Unread)
                .ThenByDescending(n => n.Type == "pending")
                .Take(8)
                .ToList();

            var model = new NotificationsViewModel
            {
                Notifications = sortedNotifications,
                UnreadCount = sortedNotifications.Count(n => n.Unread)
            };

            return View(model);
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
        public string Type { get; set; } = "info"; // announcement, pending, approval, rejected, urgent, discussion
        public bool Unread { get; set; }
        public string Link { get; set; } = "#";
        public bool IsPersisted { get; set; } = false; // Whether this notification is stored in DB
    }

    public class NotificationsViewModel
    {
        public List<NotificationItem> Notifications { get; set; } = new();
        public int UnreadCount { get; set; }
    }
}
