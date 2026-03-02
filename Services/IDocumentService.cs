using JAS_MINE_IT15.Models;
using JAS_MINE_IT15.Models.Entities;

namespace JAS_MINE_IT15.Services
{
    /// <summary>
    /// Service interface for document and knowledge-repository operations.
    /// Encapsulates business rules, tenant filtering, and pagination.
    /// </summary>
    public interface IDocumentService
    {
        Task<PagedResult<KnowledgeDocument>> GetDocumentsAsync(
            string? search = null, string? category = null, string? status = null,
            int page = 1, int pageSize = 20);

        Task<KnowledgeDocument?> GetByIdAsync(int id);
        Task<List<string>> GetCategoriesAsync();
        Task<int> GetTotalCountAsync();
        Task<int> GetPendingCountAsync();
    }
}
