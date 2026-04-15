namespace JAS_MINE_IT15.Models
{
    public class RecaptchaSettings
    {
        public string SiteKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string VerifyUrl { get; set; } = "https://www.google.com/recaptcha/api/siteverify";
    }
}
