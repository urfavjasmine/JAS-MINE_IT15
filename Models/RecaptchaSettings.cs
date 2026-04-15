namespace JAS_MINE_IT15.Models
{
    /// <summary>
    /// Configuration settings for Google reCAPTCHA v2 ("I'm not a robot" checkbox).
    /// </summary>
    public class RecaptchaSettings
    {
        /// <summary>
        /// Public site key (safe to expose to client).
        /// </summary>
        public string SiteKey { get; set; } = string.Empty;

        /// <summary>
        /// Private secret key (must be kept secret - server-side only).
        /// </summary>
        public string SecretKey { get; set; } = string.Empty;

        /// <summary>
        /// Google's reCAPTCHA verification endpoint.
        /// </summary>
        public string VerifyUrl { get; set; } = "https://www.google.com/recaptcha/api/siteverify";
    }
}
