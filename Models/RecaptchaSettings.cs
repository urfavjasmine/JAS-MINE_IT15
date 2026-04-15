namespace JAS_MINE_IT15.Models
{
    /// <summary>
    /// Configuration settings for Google reCAPTCHA v3.
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
        /// Google's reCAPTCHA v3 verification endpoint.
        /// </summary>
        public string VerifyUrl { get; set; } = "https://www.google.com/recaptcha/api/siteverify";

        /// <summary>
        /// Action name for reCAPTCHA v3 (e.g., "login", "submit", "purchase").
        /// Must match the action used in grecaptcha.execute().
        /// </summary>
        public string Action { get; set; } = "login";

        /// <summary>
        /// Score threshold for reCAPTCHA v3 (0.0 to 1.0).
        /// - 1.0: Very likely legitimate traffic
        /// - 0.5: Medium confidence
        /// - 0.0: Very likely bot traffic
        /// Requests with score below this threshold are rejected.
        /// </summary>
        public float ScoreThreshold { get; set; } = 0.5f;
    }
}
