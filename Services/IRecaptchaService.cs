namespace JAS_MINE_IT15.Services
{
    /// <summary>
    /// Interface for Google reCAPTCHA v2 ("I'm not a robot" checkbox) token verification.
    /// Provides methods to validate reCAPTCHA responses from the client-side widget.
    /// </summary>
    public interface IRecaptchaService
    {
        /// <summary>
        /// Verifies a reCAPTCHA v2 token by sending it to Google's verification API.
        /// </summary>
        /// <param name="token">The reCAPTCHA response token from the client (g-recaptcha-response)</param>
        /// <param name="remoteIp">Optional: The user's IP address for enhanced verification</param>
        /// <returns>
        /// True if the token is valid and verification succeeds with Google's API.
        /// False if the token is invalid, verification fails, or the service is misconfigured.
        /// </returns>
        Task<bool> VerifyTokenAsync(string token, string? remoteIp);
    }
}
