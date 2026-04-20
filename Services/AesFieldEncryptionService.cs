using JAS_MINE_IT15.Models;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace JAS_MINE_IT15.Services
{
    public class AesFieldEncryptionService : IFieldEncryptionService
    {
        private const string Prefix = "enc:v1:";
        private const int NonceSize = 12;
        private const int TagSize = 16;
        private readonly byte[] _key;

        public bool IsEnabled { get; }

        public AesFieldEncryptionService(IOptions<FieldEncryptionSettings> settings)
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

        public string? Encrypt(string? plaintext)
        {
            if (!IsEnabled || string.IsNullOrEmpty(plaintext))
            {
                return plaintext;
            }

            if (plaintext.StartsWith(Prefix, StringComparison.Ordinal))
            {
                return plaintext;
            }

            var nonce = RandomNumberGenerator.GetBytes(NonceSize);
            var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
            var ciphertext = new byte[plaintextBytes.Length];
            var tag = new byte[TagSize];

            using var aes = new AesGcm(_key, TagSize);
            aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

            var payload = new byte[NonceSize + TagSize + ciphertext.Length];
            Buffer.BlockCopy(nonce, 0, payload, 0, NonceSize);
            Buffer.BlockCopy(tag, 0, payload, NonceSize, TagSize);
            Buffer.BlockCopy(ciphertext, 0, payload, NonceSize + TagSize, ciphertext.Length);

            return Prefix + Convert.ToBase64String(payload);
        }

        public string? Decrypt(string? ciphertext)
        {
            if (!IsEnabled || string.IsNullOrEmpty(ciphertext))
            {
                return ciphertext;
            }

            if (!ciphertext.StartsWith(Prefix, StringComparison.Ordinal))
            {
                return ciphertext;
            }

            var payloadText = ciphertext[Prefix.Length..];
            var payload = Convert.FromBase64String(payloadText);

            if (payload.Length < NonceSize + TagSize)
            {
                throw new CryptographicException("Encrypted payload is invalid.");
            }

            var nonce = payload.AsSpan(0, NonceSize).ToArray();
            var tag = payload.AsSpan(NonceSize, TagSize).ToArray();
            var encrypted = payload.AsSpan(NonceSize + TagSize).ToArray();
            var plaintext = new byte[encrypted.Length];

            using var aes = new AesGcm(_key, TagSize);
            aes.Decrypt(nonce, encrypted, tag, plaintext);

            return Encoding.UTF8.GetString(plaintext);
        }
    }
}
