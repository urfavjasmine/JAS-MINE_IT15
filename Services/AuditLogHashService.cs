using JAS_MINE_IT15.Models;
using JAS_MINE_IT15.Models.Entities;
using Microsoft.Extensions.Options;

namespace JAS_MINE_IT15.Services
{
    public class AuditLogHashService : IAuditLogHashService
    {
        private readonly ILogger<AuditLogHashService> _logger;
        private readonly byte[]? _hmacKey;

        public string WriteAlgorithmId { get; }

        public AuditLogHashService(IOptions<AuditIntegritySettings> settings, ILogger<AuditLogHashService> logger)
        {
            _logger = logger;
            var keyText = settings.Value.HmacKey?.Trim();

            if (!string.IsNullOrWhiteSpace(keyText))
            {
                try
                {
                    _hmacKey = Convert.FromBase64String(keyText);
                }
                catch (FormatException ex)
                {
                    throw new InvalidOperationException("AuditIntegrity:HmacKey must be a valid Base64 string.", ex);
                }

                if (_hmacKey.Length < 32)
                {
                    throw new InvalidOperationException("AuditIntegrity:HmacKey must decode to at least 32 bytes.");
                }

                WriteAlgorithmId = AuditLogIntegrity.HmacSha256V1Algorithm;
                _logger.LogInformation("Audit integrity hashing configured with HMAC-SHA256.");
            }
            else
            {
                WriteAlgorithmId = AuditLogIntegrity.LegacySha256Algorithm;
                _logger.LogWarning("AuditIntegrity:HmacKey not configured. Falling back to legacy SHA-256 hashing.");
            }
        }

        public string ComputeHash(AuditLog log, string? previousHash, string? algorithmId = null)
        {
            var selectedAlgorithm = string.IsNullOrWhiteSpace(algorithmId)
                ? WriteAlgorithmId
                : algorithmId.Trim();

            if (string.Equals(selectedAlgorithm, AuditLogIntegrity.HmacSha256V1Algorithm, StringComparison.OrdinalIgnoreCase))
            {
                if (_hmacKey == null)
                {
                    throw new InvalidOperationException("Cannot verify or compute HMAC hash without AuditIntegrity:HmacKey.");
                }

                return AuditLogIntegrity.ComputeHmacSha256Hash(log, previousHash, _hmacKey);
            }

            return AuditLogIntegrity.ComputeLegacySha256Hash(log, previousHash);
        }
    }
}
