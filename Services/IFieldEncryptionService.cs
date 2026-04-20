namespace JAS_MINE_IT15.Services
{
    public interface IFieldEncryptionService
    {
        bool IsEnabled { get; }
        string? Encrypt(string? plaintext);
        string? Decrypt(string? ciphertext);
    }
}
