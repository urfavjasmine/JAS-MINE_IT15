using JAS_MINE_IT15.Data;
using JAS_MINE_IT15.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace JAS_MINE_IT15.Services
{
    public class PasswordHistoryService : IPasswordHistoryService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PasswordHistoryService> _logger;

        public PasswordHistoryService(ApplicationDbContext context, ILogger<PasswordHistoryService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> IsPasswordReusedAsync(IdentityUser user, string newPassword, int historyDepth = 5, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(user.Id) || string.IsNullOrWhiteSpace(newPassword))
            {
                return false;
            }

            var hasher = new PasswordHasher<IdentityUser>();

            if (!string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                var currentMatch = hasher.VerifyHashedPassword(user, user.PasswordHash, newPassword);
                if (currentMatch is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded)
                {
                    return true;
                }
            }

            var recentHashes = await _context.PasswordHistories
                .AsNoTracking()
                .Where(h => h.IdentityUserId == user.Id)
                .OrderByDescending(h => h.CreatedAtUtc)
                .Take(Math.Max(1, historyDepth))
                .Select(h => h.PasswordHash)
                .ToListAsync(cancellationToken);

            foreach (var hash in recentHashes)
            {
                if (string.IsNullOrWhiteSpace(hash))
                {
                    continue;
                }

                var result = hasher.VerifyHashedPassword(user, hash, newPassword);
                if (result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded)
                {
                    return true;
                }
            }

            return false;
        }

        public async Task RecordPasswordAsync(IdentityUser user, int maxHistoryDepth = 5, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(user.Id) || string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                return;
            }

            var latestHash = await _context.PasswordHistories
                .Where(h => h.IdentityUserId == user.Id)
                .OrderByDescending(h => h.CreatedAtUtc)
                .Select(h => h.PasswordHash)
                .FirstOrDefaultAsync(cancellationToken);

            if (string.Equals(latestHash, user.PasswordHash, StringComparison.Ordinal))
            {
                return;
            }

            _context.PasswordHistories.Add(new PasswordHistory
            {
                IdentityUserId = user.Id,
                PasswordHash = user.PasswordHash,
                CreatedAtUtc = DateTime.UtcNow
            });

            await _context.SaveChangesAsync(cancellationToken);

            var keep = Math.Max(1, maxHistoryDepth);
            var oldEntries = await _context.PasswordHistories
                .Where(h => h.IdentityUserId == user.Id)
                .OrderByDescending(h => h.CreatedAtUtc)
                .Skip(keep)
                .ToListAsync(cancellationToken);

            if (oldEntries.Count > 0)
            {
                _context.PasswordHistories.RemoveRange(oldEntries);
                await _context.SaveChangesAsync(cancellationToken);
            }

            _logger.LogInformation("Recorded password history for user {UserId}", user.Id);
        }
    }
}
