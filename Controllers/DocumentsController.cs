using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JAS_MINE_IT15.Data;
using JAS_MINE_IT15.Filters;
using JAS_MINE_IT15.Models;
using JAS_MINE_IT15.Models.Entities;
using JAS_MINE_IT15.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JAS_MINE_IT15.Controllers
{
    /// <summary>
    /// EXAMPLE CONTROLLER: Demonstrates multi-tenant barangay filtering pattern.
    /// Copy this pattern to other module controllers.
    /// 
    /// KEY CONCEPTS:
    /// 1. Inject ITenantService for tenant-aware operations
    /// 2. Use .FilterByTenant() extension on queries for INDEX actions
    /// 3. Auto-set BarangayId on CREATE actions
    /// 4. Validate BarangayId ownership on EDIT/ARCHIVE actions
    /// 5. Use [DenyViewOnly] attribute to block council_member from modifying
    /// </summary>
    [Authorize]
    [Route("[controller]/[action]")]
    public class DocumentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantService _tenantService;

        public DocumentsController(ApplicationDbContext context, ITenantService tenantService)
        {
            _context = context;
            _tenantService = tenantService;
        }

        #region INDEX - List with Tenant Filtering

        /// <summary>
        /// INDEX: List all documents filtered by tenant.
        /// super_admin sees all; others see only their barangay's documents.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(string q = "", string category = "All Categories")
        {
            if (!_tenantService.IsLoggedIn())
                return RedirectToAction("Login", "Home");

            var role = _tenantService.GetCurrentRole();
            var canUpload = role == "barangay_secretary" || role == "barangay_admin" || role == "super_admin";
            var canApprove = role == "barangay_admin" || role == "super_admin";

            q = (q ?? "").Trim().ToLower();
            category = string.IsNullOrWhiteSpace(category) ? "All Categories" : category.Trim();

            // =============================================================
            // TENANT FILTERING PATTERN:
            // 1. Start with base query filtering IsActive
            // 2. Apply .FilterByTenant() with the BarangayId selector
            // =============================================================
            var query = _context.KnowledgeDocuments
                .Where(d => d.IsActive)
                .FilterByTenant(_tenantService, d => d.BarangayId) // <-- TENANT FILTER
                .Include(d => d.UploadedBy)
                .AsQueryable();

            // Apply search filters
            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(d =>
                    d.Title.ToLower().Contains(q) ||
                    (d.Tags ?? "").ToLower().Contains(q)
                );
            }

            if (category != "All Categories")
                query = query.Where(d => d.Category == category);

            var docs = await query
                .OrderByDescending(d => d.CreatedAt)
                .Select(d => new RepoDocument
                {
                    Id = d.Id.ToString(),
                    Title = d.Title,
                    Category = d.Category,
                    TagsCsv = d.Tags ?? "",
                    UploadedBy = d.UploadedBy != null ? d.UploadedBy.FullName : "Unknown",
                    Date = d.CreatedAt.ToString("yyyy-MM-dd"),
                    Status = d.Status,
                    Version = d.Version,
                    Description = d.Description ?? "",
                    FileName = d.FileName ?? ""
                })
                .ToListAsync();

            var vm = new KnowledgeRepositoryViewModel
            {
                SearchQuery = q,
                SelectedCategory = category,
                Categories = new List<string> { "All Categories", "Resolutions", "Ordinances", "Memorandums", "Policies", "Reports" },
                Documents = docs,
                CanUpload = canUpload,
                CanApprove = canApprove,
                SuccessMessage = TempData["Success"] as string,
                ErrorMessage = TempData["Error"] as string,
            };

            return View("~/Views/Home/KnowledgeRepository.cshtml", vm);
        }

        #endregion

        #region CREATE - Auto-set BarangayId

        /// <summary>
        /// CREATE: New document with auto-set BarangayId from current tenant.
        /// [DenyViewOnly] blocks council_member from creating.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly] // <-- Blocks council_member
        public async Task<IActionResult> Create(string title, string category, string tags, string description)
        {
            if (!_tenantService.IsLoggedIn())
                return RedirectToAction("Login", "Home");

            if (!_tenantService.CanModify())
            {
                TempData["Error"] = "You do not have permission to create documents.";
                return RedirectToAction(nameof(Index));
            }

            title = (title ?? "").Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                TempData["Error"] = "Title is required.";
                return RedirectToAction(nameof(Index));
            }

            // Get uploading user ID
            var userEmail = _tenantService.GetCurrentUserEmail();
            var uploaderId = await _context.BusinessUsers
                .Where(u => u.Email == userEmail)
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            if (uploaderId == 0)
                uploaderId = 1;

            // =============================================================
            // AUTO-SET BARANGAYID PATTERN:
            // Always set BarangayId from the current tenant on CREATE.
            // This ensures new records belong to the correct barangay.
            // =============================================================
            var doc = new KnowledgeDocument
            {
                Title = title,
                Category = string.IsNullOrWhiteSpace(category) ? "Policies" : category.Trim(),
                Tags = (tags ?? "").Trim(),
                Description = (description ?? "").Trim(),
                Status = "pending",
                Version = "1.0",
                UploadedById = uploaderId,
                BarangayId = _tenantService.GetCurrentBarangayId(), // <-- AUTO-SET TENANT
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.KnowledgeDocuments.Add(doc);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Document uploaded: \"{title}\"";
            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region EDIT - Validate Tenant Ownership

        /// <summary>
        /// EDIT: Update document with tenant ownership validation.
        /// Ensures users can only edit documents from their barangay.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
        public async Task<IActionResult> Edit(string id, string title, string category, string tags, string description)
        {
            if (!_tenantService.IsLoggedIn())
                return RedirectToAction("Login", "Home");

            if (!_tenantService.CanModify())
            {
                TempData["Error"] = "You do not have permission to edit documents.";
                return RedirectToAction(nameof(Index));
            }

            if (!int.TryParse(id, out var docId))
            {
                TempData["Error"] = "Invalid document ID.";
                return RedirectToAction(nameof(Index));
            }

            var doc = await _context.KnowledgeDocuments.FindAsync(docId);
            if (doc == null || !doc.IsActive)
            {
                TempData["Error"] = "Document not found.";
                return RedirectToAction(nameof(Index));
            }

            // =============================================================
            // TENANT OWNERSHIP VALIDATION PATTERN:
            // On EDIT, verify the record belongs to user's barangay.
            // super_admin can edit any record.
            // =============================================================
            if (!_tenantService.IsSuperAdmin())
            {
                var userBarangayId = _tenantService.GetCurrentBarangayId();
                if (doc.BarangayId != userBarangayId)
                {
                    TempData["Error"] = "You cannot edit documents from another barangay.";
                    return RedirectToAction(nameof(Index));
                }
            }

            doc.Title = (title ?? doc.Title).Trim();
            doc.Category = string.IsNullOrWhiteSpace(category) ? doc.Category : category.Trim();
            doc.Tags = (tags ?? "").Trim();
            doc.Description = (description ?? "").Trim();
            doc.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Document updated.";
            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region ARCHIVE (Soft Delete) - Validate Tenant Ownership

        /// <summary>
        /// ARCHIVE: Soft delete document with tenant ownership validation.
        /// Sets IsActive = false instead of hard delete.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
        public async Task<IActionResult> Archive(string id)
        {
            if (!_tenantService.IsLoggedIn())
                return RedirectToAction("Login", "Home");

            if (!_tenantService.CanModify())
            {
                TempData["Error"] = "You do not have permission to archive documents.";
                return RedirectToAction(nameof(Index));
            }

            if (!int.TryParse(id, out var docId))
            {
                TempData["Error"] = "Invalid document ID.";
                return RedirectToAction(nameof(Index));
            }

            var doc = await _context.KnowledgeDocuments.FindAsync(docId);
            if (doc == null)
            {
                TempData["Error"] = "Document not found.";
                return RedirectToAction(nameof(Index));
            }

            // =============================================================
            // TENANT OWNERSHIP VALIDATION PATTERN:
            // On ARCHIVE, verify the record belongs to user's barangay.
            // super_admin can archive any record.
            // =============================================================
            if (!_tenantService.IsSuperAdmin())
            {
                var userBarangayId = _tenantService.GetCurrentBarangayId();
                if (doc.BarangayId != userBarangayId)
                {
                    TempData["Error"] = "You cannot archive documents from another barangay.";
                    return RedirectToAction(nameof(Index));
                }
            }

            doc.IsActive = false;
            doc.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Document archived.";
            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region DETAILS - Get Single with Tenant Check

        /// <summary>
        /// DETAILS: Get single document with tenant ownership validation.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            if (!_tenantService.IsLoggedIn())
                return RedirectToAction("Login", "Home");

            var doc = await _context.KnowledgeDocuments
                .Include(d => d.UploadedBy)
                .FirstOrDefaultAsync(d => d.Id == id && d.IsActive);

            if (doc == null)
                return NotFound();

            // =============================================================
            // TENANT OWNERSHIP VALIDATION PATTERN:
            // On DETAILS, verify the record belongs to user's barangay.
            // super_admin can view any record.
            // =============================================================
            if (!_tenantService.IsSuperAdmin())
            {
                var userBarangayId = _tenantService.GetCurrentBarangayId();
                if (doc.BarangayId != userBarangayId)
                    return NotFound(); // Don't reveal other barangay's data
            }

            return Json(new
            {
                id = doc.Id,
                title = doc.Title,
                category = doc.Category,
                tags = doc.Tags,
                description = doc.Description,
                status = doc.Status,
                uploadedBy = doc.UploadedBy?.FullName ?? "Unknown",
                createdAt = doc.CreatedAt.ToString("yyyy-MM-dd HH:mm")
            });
        }

        #endregion

        #region APPROVE/REJECT - Admin Actions with Tenant Check

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
        public async Task<IActionResult> Approve(string id)
        {
            if (!_tenantService.IsLoggedIn())
                return RedirectToAction("Login", "Home");

            var role = _tenantService.GetCurrentRole();
            if (role != "barangay_admin" && role != "super_admin")
            {
                TempData["Error"] = "You do not have permission to approve documents.";
                return RedirectToAction(nameof(Index));
            }

            if (int.TryParse(id, out var docId))
            {
                var doc = await _context.KnowledgeDocuments.FindAsync(docId);
                if (doc != null && doc.IsActive)
                {
                    // Tenant validation for non-super admins
                    if (!_tenantService.IsSuperAdmin())
                    {
                        var userBarangayId = _tenantService.GetCurrentBarangayId();
                        if (doc.BarangayId != userBarangayId)
                        {
                            TempData["Error"] = "You cannot approve documents from another barangay.";
                            return RedirectToAction(nameof(Index));
                        }
                    }

                    doc.Status = "approved";
                    doc.ApprovedAt = DateTime.Now;
                    doc.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                }
            }

            TempData["Success"] = "Document approved.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
        public async Task<IActionResult> Reject(string id)
        {
            if (!_tenantService.IsLoggedIn())
                return RedirectToAction("Login", "Home");

            var role = _tenantService.GetCurrentRole();
            if (role != "barangay_admin" && role != "super_admin")
            {
                TempData["Error"] = "You do not have permission to reject documents.";
                return RedirectToAction(nameof(Index));
            }

            if (int.TryParse(id, out var docId))
            {
                var doc = await _context.KnowledgeDocuments.FindAsync(docId);
                if (doc != null && doc.IsActive)
                {
                    // Tenant validation
                    if (!_tenantService.IsSuperAdmin())
                    {
                        var userBarangayId = _tenantService.GetCurrentBarangayId();
                        if (doc.BarangayId != userBarangayId)
                        {
                            TempData["Error"] = "You cannot reject documents from another barangay.";
                            return RedirectToAction(nameof(Index));
                        }
                    }

                    doc.Status = "rejected";
                    doc.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                }
            }

            TempData["Success"] = "Document rejected.";
            return RedirectToAction(nameof(Index));
        }

        #endregion
    }
}
