using JAS_MINE_IT15.Models;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace JAS_MINE_IT15.Services
{
    /// <summary>
    /// Implements deterministic HMAC-SHA256 hashing for searchable encrypted fields.
    /// </summary>
    public class DeterministicEncryptionService : IDeterministicEncryptionService
    {
        private readonly byte[] _key;
        public bool IsEnabled { get; }

        public DeterministicEncryptionService(IOptions<FieldEncryptionSettings> settings)
        {
            var value = settings.Value;
            IsEnabled = value.Enabled;

            if (!IsEnabled)
            {
                _key = Array.Empty<byte>();
                return;
            }

            if (string.IsNullOrWhiteSpace(value.Key))
            {
                throw new InvalidOperationException("Field encryption is enabled but FieldEncryption:Key is missing.");
            }

            try
            {
                _key = Convert.FromBase64String(value.Key.Trim());
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException("FieldEncryption:Key must be a valid Base64 string.", ex);
            }

            if (_key.Length != 32)
            {
                throw new InvalidOperationException("FieldEncryption:Key must decode to exactly 32 bytes (AES-256). ");
            }
        }

        /// <summary>
        /// Compute deterministic HMAC-SHA256 hash (64 hex chars).
        /// Returns null for null input. Returns plaintext if encryption is disabled.
        /// </summary>
        public string ComputeHash(string? plaintext)
        {
            if (!IsEnabled)
                return plaintext ?? "";

            if (string.IsNullOrEmpty(plaintext))
                return "";

            using var hmac = new HMACSHA256(_key);
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(plaintext));
            return Convert.ToHexString(hash); // 64 hex chars for SHA256
        }
    }
}
