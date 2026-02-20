using JAS_MINE_IT15.Data;
using JAS_MINE_IT15.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JAS_MINE_IT15.Controllers.Api
{
    /// <summary>
    /// RESTful API for Announcements/Notifications management.
    /// Supports CRUD operations, filtering, and notification delivery.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AnnouncementsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AnnouncementsApiController> _logger;

        public AnnouncementsApiController(ApplicationDbContext context, ILogger<AnnouncementsApiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region DTO Classes

        public class AnnouncementDto
        {
            public int Id { get; set; }
            public string Title { get; set; } = "";
            public string Content { get; set; } = "";
            public string Priority { get; set; } = "medium";
            public string Status { get; set; } = "draft";
            public bool IsPinned { get; set; }
            public DateTime? PublishedAt { get; set; }
            public DateTime? ExpiresAt { get; set; }
            public string? Author { get; set; }
            public int? BarangayId { get; set; }
            public string? BarangayName { get; set; }
            public int ViewCount { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        public class CreateAnnouncementRequest
        {
            public string Title { get; set; } = "";
            public string Content { get; set; } = "";
            public string Priority { get; set; } = "medium";
            public string Status { get; set; } = "draft";
            public bool IsPinned { get; set; }
            public DateTime? ExpiresAt { get; set; }
            public int? BarangayId { get; set; }
            public string? TargetAudience { get; set; }
        }

        public class UpdateAnnouncementRequest
        {
            public string? Title { get; set; }
            public string? Content { get; set; }
            public string? Priority { get; set; }
            public string? Status { get; set; }
            public bool? IsPinned { get; set; }
            public DateTime? ExpiresAt { get; set; }
        }

        public class AnnouncementFilterRequest
        {
            public string? Status { get; set; }
            public string? Priority { get; set; }
            public int? BarangayId { get; set; }
            public bool? IsPinned { get; set; }
            public bool IncludeArchived { get; set; } = false;
            public int Page { get; set; } = 1;
            public int PageSize { get; set; } = 20;
        }

        public class PaginatedResponse<T>
        {
            public List<T> Data { get; set; } = new();
            public int TotalCount { get; set; }
            public int Page { get; set; }
            public int PageSize { get; set; }
            public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        }

        #endregion

        /// <summary>
        /// GET api/announcementsapi - Get all announcements with optional filtering
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PaginatedResponse<AnnouncementDto>>> GetAll([FromQuery] AnnouncementFilterRequest filter)
        {
            var query = _context.Announcements
                .Include(a => a.Author)
                .Where(a => a.IsActive)
                .AsQueryable();

            // Apply filters
            if (!filter.IncludeArchived)
                query = query.Where(a => !a.IsArchived);

            if (!string.IsNullOrWhiteSpace(filter.Status))
                query = query.Where(a => a.Status == filter.Status);

            if (!string.IsNullOrWhiteSpace(filter.Priority))
                query = query.Where(a => a.Priority == filter.Priority);

            if (filter.BarangayId.HasValue)
                query = query.Where(a => a.BarangayId == filter.BarangayId);

            if (filter.IsPinned.HasValue)
                query = query.Where(a => a.IsPinned == filter.IsPinned);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(a => a.IsPinned)
                .ThenByDescending(a => a.CreatedAt)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(a => new AnnouncementDto
                {
                    Id = a.Id,
                    Title = a.Title,
                    Content = a.Content,
                    Priority = a.Priority,
                    Status = a.Status,
                    IsPinned = a.IsPinned,
                    PublishedAt = a.PublishedAt,
                    ExpiresAt = a.ExpiresAt,
                    Author = a.Author != null ? a.Author.FullName : null,
                    BarangayId = a.BarangayId,
                    ViewCount = a.ViewCount,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();

            return Ok(new PaginatedResponse<AnnouncementDto>
            {
                Data = items,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            });
        }

        /// <summary>
        /// GET api/announcementsapi/{id} - Get single announcement by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<AnnouncementDto>> GetById(int id)
        {
            var announcement = await _context.Announcements
                .Include(a => a.Author)
                .FirstOrDefaultAsync(a => a.Id == id && a.IsActive);

            if (announcement == null)
                return NotFound(new { error = "Announcement not found" });

            return Ok(new AnnouncementDto
            {
                Id = announcement.Id,
                Title = announcement.Title,
                Content = announcement.Content,
                Priority = announcement.Priority,
                Status = announcement.Status,
                IsPinned = announcement.IsPinned,
                PublishedAt = announcement.PublishedAt,
                ExpiresAt = announcement.ExpiresAt,
                Author = announcement.Author?.FullName,
                BarangayId = announcement.BarangayId,
                ViewCount = announcement.ViewCount,
                CreatedAt = announcement.CreatedAt
            });
        }

        /// <summary>
        /// POST api/announcementsapi - Create new announcement
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<AnnouncementDto>> Create([FromBody] CreateAnnouncementRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                return BadRequest(new { error = "Title is required" });

            var userId = HttpContext.Session.GetInt32("UserId") ?? 1;

            var announcement = new Announcement
            {
                Title = request.Title.Trim(),
                Content = request.Content?.Trim() ?? "",
                Priority = request.Priority ?? "medium",
                Status = request.Status ?? "draft",
                IsPinned = request.IsPinned,
                ExpiresAt = request.ExpiresAt,
                AuthorId = userId,
                BarangayId = request.BarangayId,
                TargetAudience = request.TargetAudience,
                IsActive = true,
                IsArchived = false,
                CreatedAt = DateTime.Now
            };

            if (announcement.Status == "published")
                announcement.PublishedAt = DateTime.Now;

            _context.Announcements.Add(announcement);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Announcement created: {Id} - {Title}", announcement.Id, announcement.Title);

            return CreatedAtAction(nameof(GetById), new { id = announcement.Id }, new AnnouncementDto
            {
                Id = announcement.Id,
                Title = announcement.Title,
                Content = announcement.Content,
                Priority = announcement.Priority,
                Status = announcement.Status,
                IsPinned = announcement.IsPinned,
                CreatedAt = announcement.CreatedAt
            });
        }

        /// <summary>
        /// PUT api/announcementsapi/{id} - Update announcement
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<AnnouncementDto>> Update(int id, [FromBody] UpdateAnnouncementRequest request)
        {
            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement == null || !announcement.IsActive)
                return NotFound(new { error = "Announcement not found" });

            if (!string.IsNullOrWhiteSpace(request.Title))
                announcement.Title = request.Title.Trim();

            if (!string.IsNullOrWhiteSpace(request.Content))
                announcement.Content = request.Content.Trim();

            if (!string.IsNullOrWhiteSpace(request.Priority))
                announcement.Priority = request.Priority;

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                if (request.Status == "published" && announcement.Status != "published")
                    announcement.PublishedAt = DateTime.Now;
                announcement.Status = request.Status;
            }

            if (request.IsPinned.HasValue)
                announcement.IsPinned = request.IsPinned.Value;

            if (request.ExpiresAt.HasValue)
                announcement.ExpiresAt = request.ExpiresAt;

            announcement.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Announcement updated: {Id}", id);

            return Ok(new AnnouncementDto
            {
                Id = announcement.Id,
                Title = announcement.Title,
                Content = announcement.Content,
                Priority = announcement.Priority,
                Status = announcement.Status,
                IsPinned = announcement.IsPinned,
                CreatedAt = announcement.CreatedAt
            });
        }

        /// <summary>
        /// DELETE api/announcementsapi/{id} - Soft delete (archive) announcement
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement == null)
                return NotFound(new { error = "Announcement not found" });

            announcement.IsArchived = true;
            announcement.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Announcement archived: {Id}", id);

            return Ok(new { message = "Announcement archived successfully" });
        }

        /// <summary>
        /// POST api/announcementsapi/{id}/publish - Publish announcement
        /// </summary>
        [HttpPost("{id}/publish")]
        public async Task<IActionResult> Publish(int id)
        {
            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement == null || !announcement.IsActive)
                return NotFound(new { error = "Announcement not found" });

            announcement.Status = "published";
            announcement.PublishedAt = DateTime.Now;
            announcement.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Announcement published", publishedAt = announcement.PublishedAt });
        }

        /// <summary>
        /// POST api/announcementsapi/{id}/pin - Toggle pin status
        /// </summary>
        [HttpPost("{id}/pin")]
        public async Task<IActionResult> TogglePin(int id)
        {
            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement == null || !announcement.IsActive)
                return NotFound(new { error = "Announcement not found" });

            announcement.IsPinned = !announcement.IsPinned;
            announcement.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Ok(new { message = announcement.IsPinned ? "Announcement pinned" : "Announcement unpinned", isPinned = announcement.IsPinned });
        }

        /// <summary>
        /// GET api/announcementsapi/notifications - Get unread notifications for current user
        /// </summary>
        [HttpGet("notifications")]
        public async Task<ActionResult<object>> GetNotifications()
        {
            var barangayId = HttpContext.Session.GetInt32("BarangayId");

            var notifications = await _context.Announcements
                .Where(a => a.IsActive && !a.IsArchived && a.Status == "published")
                .Where(a => a.BarangayId == null || a.BarangayId == barangayId)
                .OrderByDescending(a => a.IsPinned)
                .ThenByDescending(a => a.PublishedAt)
                .Take(10)
                .Select(a => new
                {
                    a.Id,
                    a.Title,
                    a.Priority,
                    a.IsPinned,
                    PublishedAt = a.PublishedAt,
                    IsNew = a.PublishedAt > DateTime.Now.AddDays(-1)
                })
                .ToListAsync();

            return Ok(new
            {
                count = notifications.Count,
                unreadCount = notifications.Count(n => n.IsNew),
                notifications
            });
        }
    }
}
