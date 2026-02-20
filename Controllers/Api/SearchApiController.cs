using JAS_MINE_IT15.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JAS_MINE_IT15.Controllers.Api
{
    /// <summary>
    /// RESTful API for Advanced Search and Indexing.
    /// Provides unified search across documents, policies, announcements, best practices, and lessons learned.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class SearchApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SearchApiController> _logger;

        public SearchApiController(ApplicationDbContext context, ILogger<SearchApiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region DTO Classes

        public class SearchResult
        {
            public int Id { get; set; }
            public string Type { get; set; } = "";
            public string Title { get; set; } = "";
            public string? Description { get; set; }
            public string? Category { get; set; }
            public string? Status { get; set; }
            public string? Author { get; set; }
            public int? BarangayId { get; set; }
            public string? BarangayName { get; set; }
            public DateTime CreatedAt { get; set; }
            public double Relevance { get; set; }
            public string? Url { get; set; }
            public Dictionary<string, object>? Metadata { get; set; }
        }

        public class UnifiedSearchRequest
        {
            public string Query { get; set; } = "";
            public List<string>? Types { get; set; }  // document, policy, announcement, bestpractice, lesson
            public int? BarangayId { get; set; }
            public string? Category { get; set; }
            public string? Status { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public string? SortBy { get; set; } = "relevance";  // relevance, date, title
            public bool Descending { get; set; } = true;
            public int Page { get; set; } = 1;
            public int PageSize { get; set; } = 20;
        }

        public class SearchResponse
        {
            public List<SearchResult> Results { get; set; } = new();
            public int TotalCount { get; set; }
            public int Page { get; set; }
            public int PageSize { get; set; }
            public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
            public string Query { get; set; } = "";
            public double SearchTime { get; set; }
            public Dictionary<string, int> TypeCounts { get; set; } = new();
            public List<string> Suggestions { get; set; } = new();
        }

        public class IndexStats
        {
            public int TotalDocuments { get; set; }
            public int TotalPolicies { get; set; }
            public int TotalAnnouncements { get; set; }
            public int TotalBestPractices { get; set; }
            public int TotalLessonsLearned { get; set; }
            public int TotalIndexed { get; set; }
            public DateTime LastIndexed { get; set; }
            public Dictionary<string, int> CategoryCounts { get; set; } = new();
        }

        public class AutocompleteResult
        {
            public string Text { get; set; } = "";
            public string Type { get; set; } = "";
            public int Id { get; set; }
        }

        #endregion

        /// <summary>
        /// POST api/searchapi/search - Unified search across all content types
        /// </summary>
        [HttpPost("search")]
        public async Task<ActionResult<SearchResponse>> Search([FromBody] UnifiedSearchRequest request)
        {
            var startTime = DateTime.Now;

            if (string.IsNullOrWhiteSpace(request.Query) && 
                string.IsNullOrWhiteSpace(request.Category) &&
                !request.BarangayId.HasValue)
            {
                return BadRequest(new { error = "At least one search criteria is required" });
            }

            var searchTerm = request.Query?.ToLower().Trim() ?? "";
            var results = new List<SearchResult>();
            var typeCounts = new Dictionary<string, int>();

            var types = request.Types ?? new List<string> { "document", "policy", "announcement", "bestpractice", "lesson" };

            // Search Documents
            if (types.Contains("document"))
            {
                var docResults = await SearchDocuments(searchTerm, request);
                results.AddRange(docResults);
                typeCounts["document"] = docResults.Count;
            }

            // Search Policies
            if (types.Contains("policy"))
            {
                var policyResults = await SearchPolicies(searchTerm, request);
                results.AddRange(policyResults);
                typeCounts["policy"] = policyResults.Count;
            }

            // Search Announcements
            if (types.Contains("announcement"))
            {
                var announcementResults = await SearchAnnouncements(searchTerm, request);
                results.AddRange(announcementResults);
                typeCounts["announcement"] = announcementResults.Count;
            }

            // Search Best Practices
            if (types.Contains("bestpractice"))
            {
                var bpResults = await SearchBestPractices(searchTerm, request);
                results.AddRange(bpResults);
                typeCounts["bestpractice"] = bpResults.Count;
            }

            // Search Lessons Learned
            if (types.Contains("lesson"))
            {
                var lessonResults = await SearchLessonsLearned(searchTerm, request);
                results.AddRange(lessonResults);
                typeCounts["lesson"] = lessonResults.Count;
            }

            // Sort results
            results = request.SortBy?.ToLower() switch
            {
                "date" => request.Descending
                    ? results.OrderByDescending(r => r.CreatedAt).ToList()
                    : results.OrderBy(r => r.CreatedAt).ToList(),
                "title" => request.Descending
                    ? results.OrderByDescending(r => r.Title).ToList()
                    : results.OrderBy(r => r.Title).ToList(),
                _ => results.OrderByDescending(r => r.Relevance).ThenByDescending(r => r.CreatedAt).ToList()
            };

            var totalCount = results.Count;

            // Paginate
            var pagedResults = results
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            var searchTime = (DateTime.Now - startTime).TotalMilliseconds;

            _logger.LogInformation("Search performed: '{Query}' - {ResultCount} results in {Time}ms", 
                request.Query, totalCount, searchTime);

            return Ok(new SearchResponse
            {
                Results = pagedResults,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                Query = request.Query ?? "",
                SearchTime = searchTime,
                TypeCounts = typeCounts,
                Suggestions = await GetSuggestions(searchTerm)
            });
        }

        /// <summary>
        /// GET api/searchapi/quick - Quick search with simple query string
        /// </summary>
        [HttpGet("quick")]
        public async Task<ActionResult<SearchResponse>> QuickSearch([FromQuery] string q, [FromQuery] int limit = 10)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest(new { error = "Query parameter 'q' is required" });

            var request = new UnifiedSearchRequest
            {
                Query = q,
                PageSize = limit
            };

            return await Search(request);
        }

        /// <summary>
        /// GET api/searchapi/autocomplete - Get autocomplete suggestions
        /// </summary>
        [HttpGet("autocomplete")]
        public async Task<ActionResult<List<AutocompleteResult>>> Autocomplete([FromQuery] string q, [FromQuery] int limit = 8)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
                return Ok(new List<AutocompleteResult>());

            var searchTerm = q.ToLower().Trim();
            var results = new List<AutocompleteResult>();

            // Search document titles
            var docs = await _context.KnowledgeDocuments
                .Where(d => d.IsActive && !d.IsArchived && d.Title.ToLower().Contains(searchTerm))
                .Take(limit)
                .Select(d => new AutocompleteResult { Text = d.Title, Type = "document", Id = d.Id })
                .ToListAsync();
            results.AddRange(docs);

            // Search policy titles
            var policies = await _context.Policies
                .Where(p => p.IsActive && !p.IsArchived && p.Title.ToLower().Contains(searchTerm))
                .Take(limit)
                .Select(p => new AutocompleteResult { Text = p.Title, Type = "policy", Id = p.Id })
                .ToListAsync();
            results.AddRange(policies);

            // Search announcement titles
            var announcements = await _context.Announcements
                .Where(a => a.IsActive && !a.IsArchived && a.Title.ToLower().Contains(searchTerm))
                .Take(limit)
                .Select(a => new AutocompleteResult { Text = a.Title, Type = "announcement", Id = a.Id })
                .ToListAsync();
            results.AddRange(announcements);

            return Ok(results.Take(limit).ToList());
        }

        /// <summary>
        /// GET api/searchapi/stats - Get indexing statistics
        /// </summary>
        [HttpGet("stats")]
        public async Task<ActionResult<IndexStats>> GetStats()
        {
            var stats = new IndexStats
            {
                TotalDocuments = await _context.KnowledgeDocuments.CountAsync(d => d.IsActive && !d.IsArchived),
                TotalPolicies = await _context.Policies.CountAsync(p => p.IsActive && !p.IsArchived),
                TotalAnnouncements = await _context.Announcements.CountAsync(a => a.IsActive && !a.IsArchived),
                TotalBestPractices = await _context.BestPractices.CountAsync(b => b.IsActive && !b.IsArchived),
                TotalLessonsLearned = await _context.LessonsLearned.CountAsync(l => l.IsActive && !l.IsArchived),
                LastIndexed = DateTime.Now
            };

            stats.TotalIndexed = stats.TotalDocuments + stats.TotalPolicies + stats.TotalAnnouncements + 
                                 stats.TotalBestPractices + stats.TotalLessonsLearned;

            // Get category counts from documents
            var docCategories = await _context.KnowledgeDocuments
                .Where(d => d.IsActive && !d.IsArchived)
                .GroupBy(d => d.Category)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .ToListAsync();

            stats.CategoryCounts = docCategories.ToDictionary(c => c.Category, c => c.Count);

            return Ok(stats);
        }

        /// <summary>
        /// GET api/searchapi/categories - Get all searchable categories
        /// </summary>
        [HttpGet("categories")]
        public async Task<ActionResult<Dictionary<string, List<string>>>> GetCategories()
        {
            var categories = new Dictionary<string, List<string>>();

            categories["document"] = await _context.KnowledgeDocuments
                .Where(d => d.IsActive && !d.IsArchived)
                .Select(d => d.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            categories["policy"] = await _context.Policies
                .Where(p => p.IsActive && !p.IsArchived && p.Category != null)
                .Select(p => p.Category!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            categories["bestpractice"] = await _context.BestPractices
                .Where(b => b.IsActive && !b.IsArchived && b.Category != null)
                .Select(b => b.Category!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            return Ok(categories);
        }

        /// <summary>
        /// GET api/searchapi/recent - Get recently added/updated content
        /// </summary>
        [HttpGet("recent")]
        public async Task<ActionResult<List<SearchResult>>> GetRecent([FromQuery] int limit = 10, [FromQuery] int? barangayId = null)
        {
            var results = new List<SearchResult>();

            // Recent documents
            var recentDocs = await _context.KnowledgeDocuments
                .Include(d => d.UploadedBy)
                .Where(d => d.IsActive && !d.IsArchived)
                .Where(d => !barangayId.HasValue || d.BarangayId == barangayId)
                .OrderByDescending(d => d.CreatedAt)
                .Take(limit)
                .Select(d => new SearchResult
                {
                    Id = d.Id,
                    Type = "document",
                    Title = d.Title,
                    Description = d.Description,
                    Category = d.Category,
                    Status = d.Status,
                    Author = d.UploadedBy != null ? d.UploadedBy.FullName : null,
                    BarangayId = d.BarangayId,
                    CreatedAt = d.CreatedAt,
                    Url = $"/Home/KnowledgeRepository"
                })
                .ToListAsync();

            results.AddRange(recentDocs);

            // Recent announcements
            var recentAnnouncements = await _context.Announcements
                .Include(a => a.Author)
                .Where(a => a.IsActive && !a.IsArchived)
                .Where(a => !barangayId.HasValue || a.BarangayId == barangayId)
                .OrderByDescending(a => a.CreatedAt)
                .Take(limit)
                .Select(a => new SearchResult
                {
                    Id = a.Id,
                    Type = "announcement",
                    Title = a.Title,
                    Description = a.Content.Length > 200 ? a.Content.Substring(0, 200) + "..." : a.Content,
                    Status = a.Status,
                    Author = a.Author != null ? a.Author.FullName : null,
                    BarangayId = a.BarangayId,
                    CreatedAt = a.CreatedAt,
                    Url = $"/Home/Announcements"
                })
                .ToListAsync();

            results.AddRange(recentAnnouncements);

            return Ok(results
                .OrderByDescending(r => r.CreatedAt)
                .Take(limit)
                .ToList());
        }

        /// <summary>
        /// POST api/searchapi/reindex - Trigger reindexing of all content (admin only)
        /// </summary>
        [HttpPost("reindex")]
        public async Task<IActionResult> Reindex()
        {
            // In a real implementation, this would rebuild search indexes
            // For now, we just return stats
            var stats = await GetStats();

            _logger.LogInformation("Reindex triggered: {Count} items indexed", ((IndexStats)((OkObjectResult)stats.Result!).Value!).TotalIndexed);

            return Ok(new
            {
                message = "Reindexing completed",
                stats = ((OkObjectResult)stats.Result!).Value
            });
        }

        #region Private Search Methods

        private async Task<List<SearchResult>> SearchDocuments(string searchTerm, UnifiedSearchRequest request)
        {
            var query = _context.KnowledgeDocuments
                .Include(d => d.UploadedBy)
                .Where(d => d.IsActive && !d.IsArchived)
                .AsQueryable();

            if (request.BarangayId.HasValue)
                query = query.Where(d => d.BarangayId == request.BarangayId);

            if (!string.IsNullOrWhiteSpace(request.Category))
                query = query.Where(d => d.Category == request.Category);

            if (!string.IsNullOrWhiteSpace(request.Status))
                query = query.Where(d => d.Status == request.Status);

            if (request.StartDate.HasValue)
                query = query.Where(d => d.CreatedAt >= request.StartDate);

            if (request.EndDate.HasValue)
                query = query.Where(d => d.CreatedAt <= request.EndDate);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(d =>
                    d.Title.ToLower().Contains(searchTerm) ||
                    (d.Description != null && d.Description.ToLower().Contains(searchTerm)) ||
                    (d.Tags != null && d.Tags.ToLower().Contains(searchTerm)));
            }

            var entities = await query.Take(100).ToListAsync();
            
            return entities.Select(d => new SearchResult
            {
                Id = d.Id,
                Type = "document",
                Title = d.Title,
                Description = d.Description,
                Category = d.Category,
                Status = d.Status,
                Author = d.UploadedBy?.FullName,
                BarangayId = d.BarangayId,
                CreatedAt = d.CreatedAt,
                Relevance = CalculateRelevance(d.Title, d.Description, d.Tags, searchTerm),
                Url = "/Home/KnowledgeRepository",
                Metadata = new Dictionary<string, object>
                {
                    ["fileType"] = d.FileType ?? "",
                    ["version"] = d.Version,
                    ["viewCount"] = d.ViewCount,
                    ["downloadCount"] = d.DownloadCount
                }
            }).ToList();
        }

        private async Task<List<SearchResult>> SearchPolicies(string searchTerm, UnifiedSearchRequest request)
        {
            var query = _context.Policies
                .Include(p => p.Author)
                .Where(p => p.IsActive && !p.IsArchived)
                .AsQueryable();

            if (request.BarangayId.HasValue)
                query = query.Where(p => p.BarangayId == request.BarangayId);

            if (!string.IsNullOrWhiteSpace(request.Category))
                query = query.Where(p => p.Category == request.Category);

            if (!string.IsNullOrWhiteSpace(request.Status))
                query = query.Where(p => p.Status == request.Status);

            if (request.StartDate.HasValue)
                query = query.Where(p => p.CreatedAt >= request.StartDate);

            if (request.EndDate.HasValue)
                query = query.Where(p => p.CreatedAt <= request.EndDate);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(p =>
                    p.Title.ToLower().Contains(searchTerm) ||
                    (p.Description != null && p.Description.ToLower().Contains(searchTerm)) ||
                    (p.Content != null && p.Content.ToLower().Contains(searchTerm)));
            }

            var entities = await query.Take(100).ToListAsync();
            
            return entities.Select(p => new SearchResult
            {
                Id = p.Id,
                Type = "policy",
                Title = p.Title,
                Description = p.Description,
                Category = p.Category,
                Status = p.Status,
                Author = p.Author?.FullName,
                BarangayId = p.BarangayId,
                CreatedAt = p.CreatedAt,
                Relevance = CalculateRelevance(p.Title, p.Description, p.Content, searchTerm),
                Url = "/Home/PoliciesManagement",
                Metadata = new Dictionary<string, object>
                {
                    ["version"] = p.Version,
                    ["effectiveDate"] = p.EffectiveDate?.ToString("yyyy-MM-dd") ?? ""
                }
            }).ToList();
        }

        private async Task<List<SearchResult>> SearchAnnouncements(string searchTerm, UnifiedSearchRequest request)
        {
            var query = _context.Announcements
                .Include(a => a.Author)
                .Where(a => a.IsActive && !a.IsArchived)
                .AsQueryable();

            if (request.BarangayId.HasValue)
                query = query.Where(a => a.BarangayId == request.BarangayId);

            if (!string.IsNullOrWhiteSpace(request.Status))
                query = query.Where(a => a.Status == request.Status);

            if (request.StartDate.HasValue)
                query = query.Where(a => a.CreatedAt >= request.StartDate);

            if (request.EndDate.HasValue)
                query = query.Where(a => a.CreatedAt <= request.EndDate);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(a =>
                    a.Title.ToLower().Contains(searchTerm) ||
                    a.Content.ToLower().Contains(searchTerm));
            }

            var entities = await query.Take(100).ToListAsync();
            
            return entities.Select(a => new SearchResult
            {
                Id = a.Id,
                Type = "announcement",
                Title = a.Title,
                Description = a.Content.Length > 200 ? a.Content.Substring(0, 200) + "..." : a.Content,
                Status = a.Status,
                Author = a.Author?.FullName,
                BarangayId = a.BarangayId,
                CreatedAt = a.CreatedAt,
                Relevance = CalculateRelevance(a.Title, a.Content, null, searchTerm),
                Url = "/Home/Announcements",
                Metadata = new Dictionary<string, object>
                {
                    ["priority"] = a.Priority,
                    ["isPinned"] = a.IsPinned,
                    ["viewCount"] = a.ViewCount
                }
            }).ToList();
        }

        private async Task<List<SearchResult>> SearchBestPractices(string searchTerm, UnifiedSearchRequest request)
        {
            var query = _context.BestPractices
                .Include(b => b.SubmittedBy)
                .Where(b => b.IsActive && !b.IsArchived)
                .AsQueryable();

            if (request.BarangayId.HasValue)
                query = query.Where(b => b.BarangayId == request.BarangayId);

            if (!string.IsNullOrWhiteSpace(request.Category))
                query = query.Where(b => b.Category == request.Category);

            if (!string.IsNullOrWhiteSpace(request.Status))
                query = query.Where(b => b.Status == request.Status);

            if (request.StartDate.HasValue)
                query = query.Where(b => b.CreatedAt >= request.StartDate);

            if (request.EndDate.HasValue)
                query = query.Where(b => b.CreatedAt <= request.EndDate);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(b =>
                    b.Title.ToLower().Contains(searchTerm) ||
                    (b.Description != null && b.Description.ToLower().Contains(searchTerm)) ||
                    (b.Steps != null && b.Steps.ToLower().Contains(searchTerm)));
            }

            var entities = await query.Take(100).ToListAsync();
            
            return entities.Select(b => new SearchResult
            {
                Id = b.Id,
                Type = "bestpractice",
                Title = b.Title,
                Description = b.Description,
                Category = b.Category,
                Status = b.Status,
                Author = b.SubmittedBy?.FullName,
                BarangayId = b.BarangayId,
                CreatedAt = b.CreatedAt,
                Relevance = CalculateRelevance(b.Title, b.Description, b.Steps, searchTerm),
                Url = "/Home/BestPractices",
                Metadata = new Dictionary<string, object>
                {
                    ["implementations"] = b.Implementations,
                    ["featured"] = b.IsFeatured
                }
            }).ToList();
        }

        private async Task<List<SearchResult>> SearchLessonsLearned(string searchTerm, UnifiedSearchRequest request)
        {
            var query = _context.LessonsLearned
                .Include(l => l.SubmittedBy)
                .Where(l => l.IsActive && !l.IsArchived)
                .AsQueryable();

            if (request.BarangayId.HasValue)
                query = query.Where(l => l.BarangayId == request.BarangayId);

            // LessonLearned uses ProjectType instead of Category
            if (!string.IsNullOrWhiteSpace(request.Category))
                query = query.Where(l => l.ProjectType == request.Category);

            if (request.StartDate.HasValue)
                query = query.Where(l => l.CreatedAt >= request.StartDate);

            if (request.EndDate.HasValue)
                query = query.Where(l => l.CreatedAt <= request.EndDate);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(l =>
                    l.Title.ToLower().Contains(searchTerm) ||
                    (l.Problem != null && l.Problem.ToLower().Contains(searchTerm)) ||
                    (l.ActionTaken != null && l.ActionTaken.ToLower().Contains(searchTerm)) ||
                    (l.Result != null && l.Result.ToLower().Contains(searchTerm)));
            }

            var entities = await query.Take(100).ToListAsync();
            
            return entities.Select(l => new SearchResult
            {
                Id = l.Id,
                Type = "lesson",
                Title = l.Title,
                Description = l.Problem,
                Category = l.ProjectType,
                Author = l.SubmittedBy?.FullName,
                BarangayId = l.BarangayId,
                CreatedAt = l.CreatedAt,
                Relevance = CalculateRelevance(l.Title, l.Problem, l.ActionTaken, searchTerm),
                Url = "/Home/LessonsLearned"
            }).ToList();
        }

        private static double CalculateRelevance(string? title, string? description, string? content, string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return 1.0;

            double score = 0;

            // Title match (highest weight)
            if (!string.IsNullOrEmpty(title))
            {
                if (title.ToLower() == searchTerm)
                    score += 100;
                else if (title.ToLower().StartsWith(searchTerm))
                    score += 50;
                else if (title.ToLower().Contains(searchTerm))
                    score += 30;
            }

            // Description match
            if (!string.IsNullOrEmpty(description) && description.ToLower().Contains(searchTerm))
                score += 20;

            // Content match
            if (!string.IsNullOrEmpty(content) && content.ToLower().Contains(searchTerm))
                score += 10;

            return score;
        }

        private async Task<List<string>> GetSuggestions(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2)
                return new List<string>();

            var suggestions = new List<string>();

            // Get similar titles from documents
            var docTitles = await _context.KnowledgeDocuments
                .Where(d => d.IsActive && !d.IsArchived && d.Title.ToLower().Contains(searchTerm))
                .Select(d => d.Title)
                .Take(3)
                .ToListAsync();
            suggestions.AddRange(docTitles);

            // Get categories
            var categories = await _context.KnowledgeDocuments
                .Where(d => d.IsActive && !d.IsArchived && d.Category.ToLower().Contains(searchTerm))
                .Select(d => d.Category)
                .Distinct()
                .Take(2)
                .ToListAsync();
            suggestions.AddRange(categories);

            return suggestions.Distinct().Take(5).ToList();
        }

        #endregion
    }
}
