using Microsoft.AspNetCore.Identity;

namespace JAS_MINE_IT15.Services
{
    public interface IPasswordHistoryService
    {
        Task<bool> IsPasswordReusedAsync(IdentityUser user, string newPassword, int historyDepth = 5, CancellationToken cancellationToken = default);
        Task RecordPasswordAsync(IdentityUser user, int maxHistoryDepth = 5, CancellationToken cancellationToken = default);
    }
}
