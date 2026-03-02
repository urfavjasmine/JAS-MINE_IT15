using JAS_MINE_IT15.Data;
using JAS_MINE_IT15.Models;
using JAS_MINE_IT15.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace JAS_MINE_IT15.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantService _tenantService;

        public DocumentService(ApplicationDbContext context, ITenantService tenantService)
        {
            _context = context;
            _tenantService = tenantService;
        }

        public async Task<PagedResult<KnowledgeDocument>> GetDocumentsAsync(
            string? search = null, string? category = null, string? status = null,
            int page = 1, int pageSize = 20)
        {
            var query = _context.KnowledgeDocuments
                .Include(d => d.UploadedBy)
                .Where(d => d.IsActive && !d.IsArchived)
                .FilterByTenant(_tenantService, d => d.BarangayId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.ToLower();
                query = query.Where(d =>
                    d.Title.ToLower().Contains(term) ||
                    (d.Description != null && d.Description.ToLower().Contains(term)) ||
                    (d.Tags != null && d.Tags.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(category) && category != "all")
                query = query.Where(d => d.Category == category);

            if (!string.IsNullOrWhiteSpace(status) && status != "all")
                query = query.Where(d => d.Status == status);

            return await query
                .OrderByDescending(d => d.CreatedAt)
                .ToPagedResultAsync(page, pageSize);
        }

        public async Task<KnowledgeDocument?> GetByIdAsync(int id)
        {
            return await _context.KnowledgeDocuments
                .Include(d => d.UploadedBy)
                .FilterByTenant(_tenantService, d => d.BarangayId)
                .FirstOrDefaultAsync(d => d.Id == id && d.IsActive);
        }

        public async Task<List<string>> GetCategoriesAsync()
        {
            return await _context.KnowledgeDocuments
                .Where(d => d.IsActive && !d.IsArchived)
                .FilterByTenant(_tenantService, d => d.BarangayId)
                .Select(d => d.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }

        public async Task<int> GetTotalCountAsync()
        {
            return await _context.KnowledgeDocuments
                .Where(d => d.IsActive && !d.IsArchived)
                .FilterByTenant(_tenantService, d => d.BarangayId)
                .CountAsync();
        }

        public async Task<int> GetPendingCountAsync()
        {
            return await _context.KnowledgeDocuments
                .Where(d => d.IsActive && !d.IsArchived && d.Status == "pending")
                .FilterByTenant(_tenantService, d => d.BarangayId)
                .CountAsync();
        }
    }
}
