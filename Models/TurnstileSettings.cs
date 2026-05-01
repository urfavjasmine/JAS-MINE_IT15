namespace JAS_MINE_IT15.Models
{
    public class TurnstileSettings
    {
        public const string SectionName = "Turnstile";
        public string SiteKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string VerifyUrl { get; set; } = "https://challenges.cloudflare.com/turnstile/v0/siteverify";
    }
}
