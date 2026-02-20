using JAS_MINE_IT15.Data;
using JAS_MINE_IT15.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace JAS_MINE_IT15.Controllers.Api
{
    /// <summary>
    /// RESTful API for Audit Log management.
    /// Provides logging, querying, and analytics for system activities.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AuditLogsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuditLogsApiController> _logger;

        public AuditLogsApiController(ApplicationDbContext context, ILogger<AuditLogsApiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region DTO Classes

        public class AuditLogDto
        {
            public long Id { get; set; }
            public int? UserId { get; set; }
            public string? UserEmail { get; set; }
            public string? UserName { get; set; }
            public string Action { get; set; } = "";
            public string Module { get; set; } = "";
            public int? TargetId { get; set; }
            public string? TargetType { get; set; }
            public string? TargetName { get; set; }
            public string? Description { get; set; }
            public object? OldValues { get; set; }
            public object? NewValues { get; set; }
            public string? IpAddress { get; set; }
            public string? UserAgent { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        public class CreateAuditLogRequest
        {
            public string Action { get; set; } = "";
            public string Module { get; set; } = "";
            public int? TargetId { get; set; }
            public string? TargetType { get; set; }
            public string? TargetName { get; set; }
            public string? Description { get; set; }
            public object? OldValues { get; set; }
            public object? NewValues { get; set; }
        }

        public class AuditLogFilterRequest
        {
            public string? Action { get; set; }
            public string? Module { get; set; }
            public int? UserId { get; set; }
            public string? UserEmail { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public string? Search { get; set; }
            public int Page { get; set; } = 1;
            public int PageSize { get; set; } = 50;
        }

        public class PaginatedResponse<T>
        {
            public List<T> Data { get; set; } = new();
            public int TotalCount { get; set; }
            public int Page { get; set; }
            public int PageSize { get; set; }
            public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        }

        public class AuditLogStats
        {
            public int TotalLogs { get; set; }
            public int TodayLogs { get; set; }
            public int ThisWeekLogs { get; set; }
            public int ThisMonthLogs { get; set; }
            public Dictionary<string, int> ActionCounts { get; set; } = new();
            public Dictionary<string, int> ModuleCounts { get; set; } = new();
            public List<UserActivitySummary> TopUsers { get; set; } = new();
        }

        public class UserActivitySummary
        {
            public int? UserId { get; set; }
            public string? UserEmail { get; set; }
            public string? UserName { get; set; }
            public int ActivityCount { get; set; }
        }

        #endregion

        /// <summary>
        /// GET api/auditlogsapi - Get audit logs with filtering
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PaginatedResponse<AuditLogDto>>> GetAll([FromQuery] AuditLogFilterRequest filter)
        {
            var query = _context.AuditLogs
                .Include(a => a.User)
                .Where(a => a.IsActive)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(filter.Action))
                query = query.Where(a => a.Action == filter.Action);

            if (!string.IsNullOrWhiteSpace(filter.Module))
                query = query.Where(a => a.Module == filter.Module);

            if (filter.UserId.HasValue)
                query = query.Where(a => a.UserId == filter.UserId);

            if (!string.IsNullOrWhiteSpace(filter.UserEmail))
                query = query.Where(a => a.UserEmail != null && a.UserEmail.Contains(filter.UserEmail));

            if (filter.StartDate.HasValue)
                query = query.Where(a => a.CreatedAt >= filter.StartDate.Value);

            if (filter.EndDate.HasValue)
                query = query.Where(a => a.CreatedAt <= filter.EndDate.Value.AddDays(1));

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var searchTerm = filter.Search.ToLower();
                query = query.Where(a =>
                    (a.Description != null && a.Description.ToLower().Contains(searchTerm)) ||
                    (a.TargetName != null && a.TargetName.ToLower().Contains(searchTerm)) ||
                    (a.UserEmail != null && a.UserEmail.ToLower().Contains(searchTerm)));
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(a => new AuditLogDto
                {
                    Id = a.Id,
                    UserId = a.UserId,
                    UserEmail = a.UserEmail,
                    UserName = a.UserName ?? (a.User != null ? a.User.FullName : null),
                    Action = a.Action,
                    Module = a.Module,
                    TargetId = a.TargetId,
                    TargetType = a.TargetType,
                    TargetName = a.TargetName,
                    Description = a.Description,
                    IpAddress = a.IpAddress,
                    UserAgent = a.UserAgent,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();

            return Ok(new PaginatedResponse<AuditLogDto>
            {
                Data = items,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            });
        }

        /// <summary>
        /// GET api/auditlogsapi/{id} - Get single audit log by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<AuditLogDto>> GetById(long id)
        {
            var log = await _context.AuditLogs
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == id && a.IsActive);

            if (log == null)
                return NotFound(new { error = "Audit log not found" });

            object? oldValues = null;
            object? newValues = null;

            try
            {
                if (!string.IsNullOrEmpty(log.OldValues))
                    oldValues = JsonSerializer.Deserialize<object>(log.OldValues);
                if (!string.IsNullOrEmpty(log.NewValues))
                    newValues = JsonSerializer.Deserialize<object>(log.NewValues);
            }
            catch { }

            return Ok(new AuditLogDto
            {
                Id = log.Id,
                UserId = log.UserId,
                UserEmail = log.UserEmail,
                UserName = log.UserName ?? log.User?.FullName,
                Action = log.Action,
                Module = log.Module,
                TargetId = log.TargetId,
                TargetType = log.TargetType,
                TargetName = log.TargetName,
                Description = log.Description,
                OldValues = oldValues,
                NewValues = newValues,
                IpAddress = log.IpAddress,
                UserAgent = log.UserAgent,
                CreatedAt = log.CreatedAt
            });
        }

        /// <summary>
        /// POST api/auditlogsapi - Create new audit log entry
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<AuditLogDto>> Create([FromBody] CreateAuditLogRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Action))
                return BadRequest(new { error = "Action is required" });

            if (string.IsNullOrWhiteSpace(request.Module))
                return BadRequest(new { error = "Module is required" });

            var userId = HttpContext.Session.GetInt32("UserId");
            var userEmail = HttpContext.Session.GetString("UserEmail");
            var userName = HttpContext.Session.GetString("UserName");

            var log = new AuditLog
            {
                UserId = userId,
                UserEmail = userEmail,
                UserName = userName,
                Action = request.Action.Trim(),
                Module = request.Module.Trim(),
                TargetId = request.TargetId,
                TargetType = request.TargetType?.Trim(),
                TargetName = request.TargetName?.Trim(),
                Description = request.Description?.Trim(),
                OldValues = request.OldValues != null ? JsonSerializer.Serialize(request.OldValues) : null,
                NewValues = request.NewValues != null ? JsonSerializer.Serialize(request.NewValues) : null,
                IpAddress = GetClientIpAddress(),
                UserAgent = Request.Headers["User-Agent"].ToString(),
                SessionId = HttpContext.Session.Id,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Audit log created: {Action} on {Module} by {User}", log.Action, log.Module, log.UserEmail);

            return CreatedAtAction(nameof(GetById), new { id = log.Id }, new AuditLogDto
            {
                Id = log.Id,
                UserId = log.UserId,
                UserEmail = log.UserEmail,
                Action = log.Action,
                Module = log.Module,
                TargetId = log.TargetId,
                TargetName = log.TargetName,
                Description = log.Description,
                CreatedAt = log.CreatedAt
            });
        }

        /// <summary>
        /// POST api/auditlogsapi/log - Quick logging endpoint for frontend
        /// </summary>
        [HttpPost("log")]
        public async Task<IActionResult> QuickLog([FromBody] CreateAuditLogRequest request)
        {
            var result = await Create(request);
            return Ok(new { success = true, message = "Activity logged" });
        }

        /// <summary>
        /// GET api/auditlogsapi/stats - Get audit log statistics
        /// </summary>
        [HttpGet("stats")]
        public async Task<ActionResult<AuditLogStats>> GetStats()
        {
            var now = DateTime.Now;
            var today = now.Date;
            var weekStart = today.AddDays(-(int)today.DayOfWeek);
            var monthStart = new DateTime(now.Year, now.Month, 1);

            var allLogs = await _context.AuditLogs.Where(a => a.IsActive).ToListAsync();

            var stats = new AuditLogStats
            {
                TotalLogs = allLogs.Count,
                TodayLogs = allLogs.Count(a => a.CreatedAt >= today),
                ThisWeekLogs = allLogs.Count(a => a.CreatedAt >= weekStart),
                ThisMonthLogs = allLogs.Count(a => a.CreatedAt >= monthStart),
                ActionCounts = allLogs
                    .GroupBy(a => a.Action)
                    .ToDictionary(g => g.Key, g => g.Count()),
                ModuleCounts = allLogs
                    .GroupBy(a => a.Module)
                    .ToDictionary(g => g.Key, g => g.Count()),
                TopUsers = allLogs
                    .Where(a => a.UserId.HasValue)
                    .GroupBy(a => new { a.UserId, a.UserEmail, a.UserName })
                    .Select(g => new UserActivitySummary
                    {
                        UserId = g.Key.UserId,
                        UserEmail = g.Key.UserEmail,
                        UserName = g.Key.UserName,
                        ActivityCount = g.Count()
                    })
                    .OrderByDescending(u => u.ActivityCount)
                    .Take(10)
                    .ToList()
            };

            return Ok(stats);
        }

        /// <summary>
        /// GET api/auditlogsapi/actions - Get all unique action types
        /// </summary>
        [HttpGet("actions")]
        public async Task<ActionResult<List<string>>> GetActions()
        {
            var actions = await _context.AuditLogs
                .Where(a => a.IsActive)
                .Select(a => a.Action)
                .Distinct()
                .OrderBy(a => a)
                .ToListAsync();

            return Ok(actions);
        }

        /// <summary>
        /// GET api/auditlogsapi/modules - Get all unique module names
        /// </summary>
        [HttpGet("modules")]
        public async Task<ActionResult<List<string>>> GetModules()
        {
            var modules = await _context.AuditLogs
                .Where(a => a.IsActive)
                .Select(a => a.Module)
                .Distinct()
                .OrderBy(m => m)
                .ToListAsync();

            return Ok(modules);
        }

        /// <summary>
        /// GET api/auditlogsapi/user/{userId} - Get audit logs for specific user
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<List<AuditLogDto>>> GetByUser(int userId, [FromQuery] int limit = 50)
        {
            var logs = await _context.AuditLogs
                .Where(a => a.IsActive && a.UserId == userId)
                .OrderByDescending(a => a.CreatedAt)
                .Take(limit)
                .Select(a => new AuditLogDto
                {
                    Id = a.Id,
                    UserId = a.UserId,
                    UserEmail = a.UserEmail,
                    UserName = a.UserName,
                    Action = a.Action,
                    Module = a.Module,
                    TargetId = a.TargetId,
                    TargetType = a.TargetType,
                    TargetName = a.TargetName,
                    Description = a.Description,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();

            return Ok(logs);
        }

        /// <summary>
        /// GET api/auditlogsapi/target/{targetType}/{targetId} - Get audit logs for specific entity
        /// </summary>
        [HttpGet("target/{targetType}/{targetId}")]
        public async Task<ActionResult<List<AuditLogDto>>> GetByTarget(string targetType, int targetId, [FromQuery] int limit = 50)
        {
            var logs = await _context.AuditLogs
                .Where(a => a.IsActive && a.TargetType == targetType && a.TargetId == targetId)
                .OrderByDescending(a => a.CreatedAt)
                .Take(limit)
                .Select(a => new AuditLogDto
                {
                    Id = a.Id,
                    UserId = a.UserId,
                    UserEmail = a.UserEmail,
                    UserName = a.UserName,
                    Action = a.Action,
                    Module = a.Module,
                    TargetId = a.TargetId,
                    TargetType = a.TargetType,
                    TargetName = a.TargetName,
                    Description = a.Description,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();

            return Ok(logs);
        }

        /// <summary>
        /// GET api/auditlogsapi/recent - Get most recent audit logs
        /// </summary>
        [HttpGet("recent")]
        public async Task<ActionResult<List<AuditLogDto>>> GetRecent([FromQuery] int limit = 20)
        {
            var logs = await _context.AuditLogs
                .Where(a => a.IsActive)
                .OrderByDescending(a => a.CreatedAt)
                .Take(limit)
                .Select(a => new AuditLogDto
                {
                    Id = a.Id,
                    UserId = a.UserId,
                    UserEmail = a.UserEmail,
                    UserName = a.UserName,
                    Action = a.Action,
                    Module = a.Module,
                    TargetId = a.TargetId,
                    TargetType = a.TargetType,
                    TargetName = a.TargetName,
                    Description = a.Description,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();

            return Ok(logs);
        }

        /// <summary>
        /// DELETE api/auditlogsapi/cleanup - Clean up old audit logs (admin only)
        /// </summary>
        [HttpDelete("cleanup")]
        public async Task<IActionResult> Cleanup([FromQuery] int daysOld = 90)
        {
            var cutoffDate = DateTime.Now.AddDays(-daysOld);

            var oldLogs = await _context.AuditLogs
                .Where(a => a.CreatedAt < cutoffDate)
                .ToListAsync();

            var count = oldLogs.Count;

            _context.AuditLogs.RemoveRange(oldLogs);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Audit log cleanup: Removed {Count} logs older than {Days} days", count, daysOld);

            return Ok(new { message = $"Removed {count} audit logs older than {daysOld} days" });
        }

        #region Helper Methods

        private string? GetClientIpAddress()
        {
            var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
                return forwardedFor.Split(',').First().Trim();

            return HttpContext.Connection.RemoteIpAddress?.ToString();
        }

        #endregion
    }
}
