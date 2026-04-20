using JAS_MINE_IT15.Models.Entities;

namespace JAS_MINE_IT15.Services
{
    public interface IAuditLogHashService
    {
        string WriteAlgorithmId { get; }
        string ComputeHash(AuditLog log, string? previousHash, string? algorithmId = null);
    }
}
