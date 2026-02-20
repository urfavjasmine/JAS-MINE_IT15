using JAS_MINE_IT15.Data;
using JAS_MINE_IT15.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JAS_MINE_IT15.Controllers.Api
{
    /// <summary>
    /// RESTful API for Document Upload and Management.
    /// Supports file upload, download, versioning, and metadata management.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class DocumentsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DocumentsApiController> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly string[] _allowedExtensions = { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".jpg", ".jpeg", ".png", ".gif" };
        private const long MaxFileSize = 50 * 1024 * 1024; // 50MB

        public DocumentsApiController(ApplicationDbContext context, ILogger<DocumentsApiController> logger, IWebHostEnvironment environment)
        {
            _context = context;
            _logger = logger;
            _environment = environment;
        }

        #region DTO Classes

        public class DocumentDto
        {
            public int Id { get; set; }
            public string Title { get; set; } = "";
            public string? Description { get; set; }
            public string Category { get; set; } = "";
            public string? Tags { get; set; }
            public string? FileName { get; set; }
            public string? FileUrl { get; set; }
            public long? FileSize { get; set; }
            public string? FileType { get; set; }
            public string Status { get; set; } = "pending";
            public string Version { get; set; } = "1.0";
            public string? UploadedBy { get; set; }
            public int? BarangayId { get; set; }
            public int ViewCount { get; set; }
            public int DownloadCount { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? ApprovedAt { get; set; }
        }

        public class UploadDocumentRequest
        {
            public string Title { get; set; } = "";
            public string? Description { get; set; }
            public string Category { get; set; } = "";
            public string? Tags { get; set; }
            public int? BarangayId { get; set; }
        }

        public class UpdateDocumentRequest
        {
            public string? Title { get; set; }
            public string? Description { get; set; }
            public string? Category { get; set; }
            public string? Tags { get; set; }
        }

        public class DocumentFilterRequest
        {
            public string? Category { get; set; }
            public string? Status { get; set; }
            public int? BarangayId { get; set; }
            public string? Search { get; set; }
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

        public class UploadResult
        {
            public bool Success { get; set; }
            public string? Message { get; set; }
            public DocumentDto? Document { get; set; }
            public string? Error { get; set; }
        }

        #endregion

        /// <summary>
        /// GET api/documentsapi - Get all documents with filtering
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PaginatedResponse<DocumentDto>>> GetAll([FromQuery] DocumentFilterRequest filter)
        {
            var query = _context.KnowledgeDocuments
                .Include(d => d.UploadedBy)
                .Where(d => d.IsActive)
                .AsQueryable();

            // Apply filters
            if (!filter.IncludeArchived)
                query = query.Where(d => !d.IsArchived);

            if (!string.IsNullOrWhiteSpace(filter.Category))
                query = query.Where(d => d.Category == filter.Category);

            if (!string.IsNullOrWhiteSpace(filter.Status))
                query = query.Where(d => d.Status == filter.Status);

            if (filter.BarangayId.HasValue)
                query = query.Where(d => d.BarangayId == filter.BarangayId);

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var searchTerm = filter.Search.ToLower();
                query = query.Where(d =>
                    d.Title.ToLower().Contains(searchTerm) ||
                    (d.Description != null && d.Description.ToLower().Contains(searchTerm)) ||
                    (d.Tags != null && d.Tags.ToLower().Contains(searchTerm)));
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(d => d.CreatedAt)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(d => new DocumentDto
                {
                    Id = d.Id,
                    Title = d.Title,
                    Description = d.Description,
                    Category = d.Category,
                    Tags = d.Tags,
                    FileName = d.FileName,
                    FileUrl = d.FileUrl,
                    FileSize = d.FileSize,
                    FileType = d.FileType,
                    Status = d.Status,
                    Version = d.Version,
                    UploadedBy = d.UploadedBy != null ? d.UploadedBy.FullName : null,
                    BarangayId = d.BarangayId,
                    ViewCount = d.ViewCount,
                    DownloadCount = d.DownloadCount,
                    CreatedAt = d.CreatedAt,
                    ApprovedAt = d.ApprovedAt
                })
                .ToListAsync();

            return Ok(new PaginatedResponse<DocumentDto>
            {
                Data = items,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            });
        }

        /// <summary>
        /// GET api/documentsapi/{id} - Get document by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<DocumentDto>> GetById(int id)
        {
            var document = await _context.KnowledgeDocuments
                .Include(d => d.UploadedBy)
                .FirstOrDefaultAsync(d => d.Id == id && d.IsActive);

            if (document == null)
                return NotFound(new { error = "Document not found" });

            // Increment view count
            document.ViewCount++;
            await _context.SaveChangesAsync();

            return Ok(new DocumentDto
            {
                Id = document.Id,
                Title = document.Title,
                Description = document.Description,
                Category = document.Category,
                Tags = document.Tags,
                FileName = document.FileName,
                FileUrl = document.FileUrl,
                FileSize = document.FileSize,
                FileType = document.FileType,
                Status = document.Status,
                Version = document.Version,
                UploadedBy = document.UploadedBy?.FullName,
                BarangayId = document.BarangayId,
                ViewCount = document.ViewCount,
                DownloadCount = document.DownloadCount,
                CreatedAt = document.CreatedAt,
                ApprovedAt = document.ApprovedAt
            });
        }

        /// <summary>
        /// POST api/documentsapi/upload - Upload new document with file
        /// </summary>
        [HttpPost("upload")]
        [RequestSizeLimit(MaxFileSize)]
        public async Task<ActionResult<UploadResult>> Upload([FromForm] UploadDocumentRequest request, IFormFile? file)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                return BadRequest(new UploadResult { Success = false, Error = "Title is required" });

            if (string.IsNullOrWhiteSpace(request.Category))
                return BadRequest(new UploadResult { Success = false, Error = "Category is required" });

            string? filePath = null;
            string? fileName = null;
            long? fileSize = null;
            string? fileType = null;

            // Handle file upload
            if (file != null && file.Length > 0)
            {
                if (file.Length > MaxFileSize)
                    return BadRequest(new UploadResult { Success = false, Error = $"File size exceeds maximum allowed ({MaxFileSize / (1024 * 1024)}MB)" });

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!_allowedExtensions.Contains(extension))
                    return BadRequest(new UploadResult { Success = false, Error = $"File type not allowed. Allowed: {string.Join(", ", _allowedExtensions)}" });

                // Create uploads directory
                var uploadsDir = Path.Combine(_environment.WebRootPath, "uploads", "documents");
                Directory.CreateDirectory(uploadsDir);

                // Generate unique filename
                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                var fullPath = Path.Combine(uploadsDir, uniqueFileName);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                filePath = $"/uploads/documents/{uniqueFileName}";
                fileName = file.FileName;
                fileSize = file.Length;
                fileType = extension.TrimStart('.');
            }

            var userId = HttpContext.Session.GetInt32("UserId") ?? 1;

            var document = new KnowledgeDocument
            {
                Title = request.Title.Trim(),
                Description = request.Description?.Trim(),
                Category = request.Category.Trim(),
                Tags = request.Tags?.Trim(),
                FileUrl = filePath,
                FileName = fileName,
                FileSize = fileSize,
                FileType = fileType,
                Status = "pending",
                Version = "1.0",
                UploadedById = userId,
                BarangayId = request.BarangayId,
                IsActive = true,
                IsArchived = false,
                CreatedAt = DateTime.Now
            };

            _context.KnowledgeDocuments.Add(document);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Document uploaded: {Id} - {Title}, File: {FileName}", document.Id, document.Title, fileName);

            return Ok(new UploadResult
            {
                Success = true,
                Message = "Document uploaded successfully",
                Document = new DocumentDto
                {
                    Id = document.Id,
                    Title = document.Title,
                    Description = document.Description,
                    Category = document.Category,
                    FileName = document.FileName,
                    FileUrl = document.FileUrl,
                    FileSize = document.FileSize,
                    FileType = document.FileType,
                    Status = document.Status,
                    CreatedAt = document.CreatedAt
                }
            });
        }

        /// <summary>
        /// PUT api/documentsapi/{id} - Update document metadata
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<DocumentDto>> Update(int id, [FromBody] UpdateDocumentRequest request)
        {
            var document = await _context.KnowledgeDocuments.FindAsync(id);
            if (document == null || !document.IsActive)
                return NotFound(new { error = "Document not found" });

            if (!string.IsNullOrWhiteSpace(request.Title))
                document.Title = request.Title.Trim();

            if (!string.IsNullOrWhiteSpace(request.Description))
                document.Description = request.Description.Trim();

            if (!string.IsNullOrWhiteSpace(request.Category))
                document.Category = request.Category.Trim();

            if (request.Tags != null)
                document.Tags = request.Tags.Trim();

            document.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Document updated: {Id}", id);

            return Ok(new DocumentDto
            {
                Id = document.Id,
                Title = document.Title,
                Description = document.Description,
                Category = document.Category,
                Tags = document.Tags,
                FileName = document.FileName,
                FileUrl = document.FileUrl,
                Status = document.Status,
                CreatedAt = document.CreatedAt
            });
        }

        /// <summary>
        /// POST api/documentsapi/{id}/replace - Replace file for existing document
        /// </summary>
        [HttpPost("{id}/replace")]
        [RequestSizeLimit(MaxFileSize)]
        public async Task<ActionResult<UploadResult>> ReplaceFile(int id, IFormFile file)
        {
            var document = await _context.KnowledgeDocuments.FindAsync(id);
            if (document == null || !document.IsActive)
                return NotFound(new UploadResult { Success = false, Error = "Document not found" });

            if (file == null || file.Length == 0)
                return BadRequest(new UploadResult { Success = false, Error = "No file provided" });

            if (file.Length > MaxFileSize)
                return BadRequest(new UploadResult { Success = false, Error = $"File size exceeds maximum allowed ({MaxFileSize / (1024 * 1024)}MB)" });

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
                return BadRequest(new UploadResult { Success = false, Error = $"File type not allowed" });

            // Delete old file if exists
            if (!string.IsNullOrEmpty(document.FileUrl))
            {
                var oldPath = Path.Combine(_environment.WebRootPath, document.FileUrl.TrimStart('/'));
                if (System.IO.File.Exists(oldPath))
                    System.IO.File.Delete(oldPath);
            }

            // Upload new file
            var uploadsDir = Path.Combine(_environment.WebRootPath, "uploads", "documents");
            Directory.CreateDirectory(uploadsDir);

            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var fullPath = Path.Combine(uploadsDir, uniqueFileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Increment version
            if (decimal.TryParse(document.Version, out var ver))
                document.Version = (ver + 0.1m).ToString("F1");
            else
                document.Version = "1.1";

            document.FileUrl = $"/uploads/documents/{uniqueFileName}";
            document.FileName = file.FileName;
            document.FileSize = file.Length;
            document.FileType = extension.TrimStart('.');
            document.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Document file replaced: {Id}, New version: {Version}", id, document.Version);

            return Ok(new UploadResult
            {
                Success = true,
                Message = $"File replaced successfully. New version: {document.Version}",
                Document = new DocumentDto
                {
                    Id = document.Id,
                    Title = document.Title,
                    FileName = document.FileName,
                    FileUrl = document.FileUrl,
                    Version = document.Version
                }
            });
        }

        /// <summary>
        /// DELETE api/documentsapi/{id} - Soft delete (archive) document
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var document = await _context.KnowledgeDocuments.FindAsync(id);
            if (document == null)
                return NotFound(new { error = "Document not found" });

            document.IsArchived = true;
            document.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Document archived: {Id}", id);

            return Ok(new { message = "Document archived successfully" });
        }

        /// <summary>
        /// POST api/documentsapi/{id}/approve - Approve document
        /// </summary>
        [HttpPost("{id}/approve")]
        public async Task<IActionResult> Approve(int id)
        {
            var document = await _context.KnowledgeDocuments.FindAsync(id);
            if (document == null || !document.IsActive)
                return NotFound(new { error = "Document not found" });

            var userId = HttpContext.Session.GetInt32("UserId") ?? 1;

            document.Status = "approved";
            document.ApprovedById = userId;
            document.ApprovedAt = DateTime.Now;
            document.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Document approved: {Id}", id);

            return Ok(new { message = "Document approved", approvedAt = document.ApprovedAt });
        }

        /// <summary>
        /// POST api/documentsapi/{id}/reject - Reject document
        /// </summary>
        [HttpPost("{id}/reject")]
        public async Task<IActionResult> Reject(int id, [FromBody] string? reason)
        {
            var document = await _context.KnowledgeDocuments.FindAsync(id);
            if (document == null || !document.IsActive)
                return NotFound(new { error = "Document not found" });

            document.Status = "rejected";
            document.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Document rejected: {Id}, Reason: {Reason}", id, reason);

            return Ok(new { message = "Document rejected", reason });
        }

        /// <summary>
        /// GET api/documentsapi/{id}/download - Download document file
        /// </summary>
        [HttpGet("{id}/download")]
        public async Task<IActionResult> Download(int id)
        {
            var document = await _context.KnowledgeDocuments.FindAsync(id);
            if (document == null || !document.IsActive || string.IsNullOrEmpty(document.FileUrl))
                return NotFound(new { error = "Document or file not found" });

            var filePath = Path.Combine(_environment.WebRootPath, document.FileUrl.TrimStart('/'));
            if (!System.IO.File.Exists(filePath))
                return NotFound(new { error = "File not found on server" });

            // Increment download count
            document.DownloadCount++;
            await _context.SaveChangesAsync();

            var contentType = document.FileType switch
            {
                "pdf" => "application/pdf",
                "doc" => "application/msword",
                "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "xls" => "application/vnd.ms-excel",
                "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "ppt" => "application/vnd.ms-powerpoint",
                "pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                "jpg" or "jpeg" => "image/jpeg",
                "png" => "image/png",
                "gif" => "image/gif",
                _ => "application/octet-stream"
            };

            return PhysicalFile(filePath, contentType, document.FileName ?? "download");
        }

        /// <summary>
        /// GET api/documentsapi/categories - Get all unique categories
        /// </summary>
        [HttpGet("categories")]
        public async Task<ActionResult<List<string>>> GetCategories()
        {
            var categories = await _context.KnowledgeDocuments
                .Where(d => d.IsActive && !d.IsArchived)
                .Select(d => d.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            return Ok(categories);
        }

        /// <summary>
        /// GET api/documentsapi/stats - Get document statistics
        /// </summary>
        [HttpGet("stats")]
        public async Task<ActionResult<object>> GetStats([FromQuery] int? barangayId)
        {
            var query = _context.KnowledgeDocuments.Where(d => d.IsActive && !d.IsArchived);

            if (barangayId.HasValue)
                query = query.Where(d => d.BarangayId == barangayId);

            var stats = await query
                .GroupBy(d => 1)
                .Select(g => new
                {
                    TotalDocuments = g.Count(),
                    PendingDocuments = g.Count(d => d.Status == "pending"),
                    ApprovedDocuments = g.Count(d => d.Status == "approved"),
                    RejectedDocuments = g.Count(d => d.Status == "rejected"),
                    TotalViews = g.Sum(d => d.ViewCount),
                    TotalDownloads = g.Sum(d => d.DownloadCount)
                })
                .FirstOrDefaultAsync();

            return Ok(stats ?? new { TotalDocuments = 0, PendingDocuments = 0, ApprovedDocuments = 0, RejectedDocuments = 0, TotalViews = 0, TotalDownloads = 0 });
        }
    }
}
