namespace JAS_MINE_IT15.Models
{
    /// <summary>
    /// Configuration settings for PayMongo payment gateway.
    /// Reads from appsettings.json or environment variables (PayMongo__SecretKey, PayMongo__PublicKey).
    /// </summary>
    public class PayMongoSettings
    {
        public const string SectionName = "PayMongo";

        /// <summary>
        /// PayMongo Secret Key (starts with sk_test_ or sk_live_)
        /// </summary>
        public string SecretKey { get; set; } = string.Empty;

        /// <summary>
        /// PayMongo Public Key (starts with pk_test_ or pk_live_)
        /// </summary>
        public string PublicKey { get; set; } = string.Empty;

        /// <summary>
        /// Optional webhook secret for signature verification
        /// </summary>
        public string? WebhookSecret { get; set; }
    }
}
