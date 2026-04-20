using JAS_MINE_IT15.Models.Entities;
using System.Security.Cryptography;
using System.Text;

namespace JAS_MINE_IT15.Services
{
    public static class AuditLogIntegrity
    {
        public static string ComputeHash(AuditLog log, string? previousHash)
        {
            var canonical = BuildCanonicalPayload(log, previousHash);
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
            return Convert.ToHexString(bytes);
        }

        private static string BuildCanonicalPayload(AuditLog log, string? previousHash)
        {
            return string.Join("|", new[]
            {
                previousHash ?? string.Empty,
                log.CreatedAt.ToUniversalTime().Ticks.ToString(),
                log.UserId?.ToString() ?? string.Empty,
                log.UserEmail ?? string.Empty,
                log.UserName ?? string.Empty,
                log.Action ?? string.Empty,
                log.Module ?? string.Empty,
                log.TargetId?.ToString() ?? string.Empty,
                log.TargetType ?? string.Empty,
                log.TargetName ?? string.Empty,
                log.Description ?? string.Empty,
                log.OldValues ?? string.Empty,
                log.NewValues ?? string.Empty,
                log.IpAddress ?? string.Empty,
                log.UserAgent ?? string.Empty,
                log.SessionId ?? string.Empty,
                log.BarangayId?.ToString() ?? string.Empty,
                log.IsActive ? "1" : "0"
            });
        }
    }
}
