using JAS_MINE_IT15.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JAS_MINE_IT15.Services
{
    /// <summary>
    /// Google reCAPTCHA v3 token verification service.
    /// Sends tokens to Google's API and validates responses based on score threshold.
    /// </summary>
    public class RecaptchaService : IRecaptchaService
    {
        private readonly HttpClient _httpClient;
        private readonly RecaptchaSettings _settings;
        private readonly ILogger<RecaptchaService> _logger;

        public RecaptchaService(HttpClient httpClient, IOptions<RecaptchaSettings> options, ILogger<RecaptchaService> logger)
        {
            _httpClient = httpClient;
            _settings = options.Value;
            _logger = logger;
        }

        /// <summary>
        /// Verifies a reCAPTCHA v3 token and checks if score meets threshold.
        /// </summary>
        public async Task<bool> VerifyTokenAsync(string token, string? remoteIp)
        {
            var (isValid, score, details) = await VerifyTokenWithScoreAsync(token, remoteIp);
            return isValid;
        }

        /// <summary>
        /// Verifies a reCAPTCHA v3 token and returns detailed score information.
        /// </summary>
        public async Task<(bool isValid, float score, string details)> VerifyTokenWithScoreAsync(string token, string? remoteIp)
        {
            // Validate token is not empty
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("reCAPTCHA verification failed: token is empty");
                return (false, -1f, "Token is empty");
            }

            // Validate secret key is configured
            if (string.IsNullOrWhiteSpace(_settings.SecretKey) || _settings.SecretKey == "REPLACE_WITH_YOUR_REAL_SECRET_KEY")
            {
                _logger.LogError("reCAPTCHA secret key is not configured or is a placeholder. " +
                    "Update appsettings.json with your actual Google reCAPTCHA secret key.");
                return (false, -1f, "reCAPTCHA is not properly configured");
            }

            try
            {
                _logger.LogDebug("reCAPTCHA v3: Sending verification request. Action: {Action}, Threshold: {Threshold}, RemoteIP: {RemoteIp}",
                    _settings.Action, _settings.ScoreThreshold, remoteIp ?? "unknown");

                // Build request payload
                var payload = new Dictionary<string, string>
                {
                    ["secret"] = _settings.SecretKey,
                    ["response"] = token
                };

                if (!string.IsNullOrWhiteSpace(remoteIp))
                    payload["remoteip"] = remoteIp;

                // Send POST request to Google's API
                using var content = new FormUrlEncodedContent(payload);
                using var response = await _httpClient.PostAsync(_settings.VerifyUrl, content);

                // Handle HTTP errors
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("reCAPTCHA API HTTP error: Status={StatusCode}, ReasonPhrase={ReasonPhrase}",
                        (int)response.StatusCode, response.ReasonPhrase);
                    return (false, -1f, $"API returned status {response.StatusCode}");
                }

                // Parse JSON response
                var json = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("reCAPTCHA API response: {Response}", json);

                var result = JsonSerializer.Deserialize<RecaptchaVerifyResponse>(json);

                if (result == null)
                {
                    _logger.LogWarning("reCAPTCHA response deserialization failed. Raw response: {Response}", json);
                    return (false, -1f, "Failed to parse API response");
                }

                // Check if token validation succeeded
                if (!result.Success)
                {
                    var errorCodes = string.Join(", ", result.ErrorCodes ?? new List<string>());
                    _logger.LogWarning("reCAPTCHA token validation failed. Error codes: {ErrorCodes}", errorCodes ?? "unknown");
                    return (false, result.Score, $"Token validation failed: {errorCodes}");
                }

                // For v3, check score against threshold
                bool scoreValid = result.Score >= _settings.ScoreThreshold;
                
                if (!scoreValid)
                {
                    _logger.LogWarning("reCAPTCHA v3 score below threshold. Score: {Score}, Threshold: {Threshold}, Action: {Action}",
                        result.Score, _settings.ScoreThreshold, result.Action);
                    return (false, result.Score, $"Score {result.Score:F2} is below threshold {_settings.ScoreThreshold:F2}");
                }

                _logger.LogDebug("reCAPTCHA v3 verification succeeded. Score: {Score}, Action: {Action}, Hostname: {Hostname}",
                    result.Score, result.Action, result.Hostname);

                return (true, result.Score, $"Valid: score {result.Score:F2}");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "reCAPTCHA HTTP request failed: {Message}", ex.Message);
                return (false, -1f, $"Network error: {ex.Message}");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "reCAPTCHA response JSON parsing failed: {Message}", ex.Message);
                return (false, -1f, $"JSON parsing error: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "reCAPTCHA verification encountered unexpected error: {Message}", ex.Message);
                return (false, -1f, $"Unexpected error: {ex.Message}");
            }
        }

        /// <summary>
        /// Models the response from Google's reCAPTCHA verification API.
        /// Works for both v2 and v3 responses.
        /// </summary>
        private sealed class RecaptchaVerifyResponse
        {
            /// <summary>
            /// Whether the token is valid and the request is legitimate.
            /// For v3, this is true if score is above threshold.
            /// </summary>
            [JsonPropertyName("success")]
            public bool Success { get; set; }

            /// <summary>
            /// The challenge timestamp in ISO format (yyyy-MM-dd'T'HH:mm:ssZ).
            /// </summary>
            [JsonPropertyName("challenge_ts")]
            public string? ChallengeTimestamp { get; set; }

            /// <summary>
            /// The hostname of the site where the reCAPTCHA was completed.
            /// </summary>
            [JsonPropertyName("hostname")]
            public string? Hostname { get; set; }

            /// <summary>
            /// Score for reCAPTCHA v3 only (0.0 to 1.0).
            /// 1.0 = very likely legitimate user
            /// 0.0 = very likely bot
            /// Not present for v2 responses.
            /// </summary>
            [JsonPropertyName("score")]
            public float Score { get; set; }

            /// <summary>
            /// The action name submitted with the reCAPTCHA v3 token.
            /// Must match the action specified in grecaptcha.execute().
            /// Not present for v2 responses.
            /// </summary>
            [JsonPropertyName("action")]
            public string? Action { get; set; }

            /// <summary>
            /// List of error codes if verification failed.
            /// Possible values:
            /// - missing-input-secret: Secret key is missing
            /// - invalid-input-secret: Secret key is invalid or malformed
            /// - missing-input-response: Response parameter is missing
            /// - invalid-input-response: Response parameter is invalid or malformed
            /// - bad-request: Request itself is invalid
            /// - timeout-or-duplicate: Response is no longer valid (too old or duplicate)
            /// </summary>
            [JsonPropertyName("error-codes")]
            public List<string>? ErrorCodes { get; set; }
        }
    }
}
