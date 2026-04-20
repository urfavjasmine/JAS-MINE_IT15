namespace JAS_MINE_IT15.Models
{
    public class AuditIntegritySettings
    {
        public const string SectionName = "AuditIntegrity";

        // Base64 encoded key for HMAC-SHA256. Use env var: AuditIntegrity__HmacKey
        public string? HmacKey { get; set; }
    }
}
