using JAS_MINE_IT15.Models.Entities;
using JAS_MINE_IT15.Models;

namespace JAS_MINE_IT15.Services
{
    /// <summary>
    /// Service for writing audit log entries.
    /// Replaces the private LogAuditAsync helper duplicated in every controller.
    /// </summary>
    public interface IAuditService
    {
        Task LogAsync(string action, string module, int? targetId, string? targetType,
            string? targetName, string description, int? barangayId = null);

        Task<AuditLogIntegrityReport> VerifyIntegrityAsync(CancellationToken cancellationToken = default);
    }
}
