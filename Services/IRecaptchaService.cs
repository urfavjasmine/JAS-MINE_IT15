namespace JAS_MINE_IT15.Services
{
    /// <summary>
    /// Interface for Google reCAPTCHA v3 token verification.
    /// Provides methods to validate reCAPTCHA responses from the client-side.
    /// </summary>
    public interface IRecaptchaService
    {
        /// <summary>
        /// Verifies a reCAPTCHA v3 token by sending it to Google's verification API.
        /// </summary>
        /// <param name="token">The reCAPTCHA response token from the client</param>
        /// <param name="remoteIp">Optional: The user's IP address for enhanced verification</param>
        /// <returns>
        /// True if the token is valid and the score meets or exceeds the threshold.
        /// False if the token is invalid, verification fails, or score is below threshold.
        /// </returns>
        Task<bool> VerifyTokenAsync(string token, string? remoteIp);

        /// <summary>
        /// Verifies a reCAPTCHA v3 token and returns detailed score information.
        /// </summary>
        /// <param name="token">The reCAPTCHA response token from the client</param>
        /// <param name="remoteIp">Optional: The user's IP address for enhanced verification</param>
        /// <returns>
        /// A tuple containing (isValid, score, details).
        /// - isValid: True if score meets threshold
        /// - score: The reCAPTCHA v3 score (0.0-1.0, or -1 if unavailable)
        /// - details: Human-readable description of the result
        /// </returns>
        Task<(bool isValid, float score, string details)> VerifyTokenWithScoreAsync(string token, string? remoteIp);
    }
}
