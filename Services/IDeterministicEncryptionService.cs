namespace JAS_MINE_IT15.Services
{
    /// <summary>
    /// Service for computing deterministic HMAC-SHA256 hashes of sensitive fields.
    /// Used for searchable encrypted fields (e.g., email uniqueness constraint).
    /// 
    /// Why: Regular encryption is non-deterministic (uses random nonce).
    /// For unique constraints on encrypted fields, we need deterministic hashes.
    /// </summary>
    public interface IDeterministicEncryptionService
    {
        /// <summary>
        /// Compute HMAC-SHA256 hash of plaintext for database indexing/search.
        /// Returns same hash for same input (deterministic).
        /// </summary>
        string ComputeHash(string? plaintext);

        bool IsEnabled { get; }
    }
}
