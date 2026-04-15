using JAS_MINE_IT15.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JAS_MINE_IT15.Services
{
    /// <summary>
    /// Google reCAPTCHA v2 ("I'm not a robot" checkbox) token verification service.
    /// Sends tokens to Google's API and validates the response.
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
        /// Verifies a reCAPTCHA v2 token by sending it to Google's verification API.
        /// </summary>
        public async Task<bool> VerifyTokenAsync(string token, string? remoteIp)
        {
            // Validate token is not empty
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("reCAPTCHA verification failed: token is empty");
                return false;
            }

            // Validate secret key is configured
            if (string.IsNullOrWhiteSpace(_settings.SecretKey) || 
                _settings.SecretKey == "REPLACE_WITH_YOUR_REAL_SECRET_KEY" ||
                _settings.SecretKey == "YOUR_SECRET_KEY_HERE")
            {
                _logger.LogError("reCAPTCHA secret key is not configured or is a placeholder. " +
                    "Update appsettings.json with your actual Google reCAPTCHA secret key.");
                return false;
            }

            try
            {
                _logger.LogDebug("reCAPTCHA v2: Sending verification request. RemoteIP: {RemoteIp}", remoteIp ?? "unknown");

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
                    return false;
                }

                // Parse JSON response
                var json = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("reCAPTCHA API response: {Response}", json);

                var result = JsonSerializer.Deserialize<RecaptchaVerifyResponse>(json);

                if (result == null)
                {
                    _logger.LogWarning("reCAPTCHA response deserialization failed. Raw response: {Response}", json);
                    return false;
                }

                // Log detailed error codes if verification failed
                if (!result.Success)
                {
                    var errorCodes = string.Join(", ", result.ErrorCodes ?? new List<string>());
                    _logger.LogWarning("reCAPTCHA verification failed. Error codes: {ErrorCodes}", errorCodes ?? "unknown");
                }
                else
                {
                    _logger.LogDebug("reCAPTCHA verification succeeded. Hostname: {Hostname}", result.Hostname);
                }

                return result.Success;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "reCAPTCHA HTTP request failed: {Message}", ex.Message);
                return false;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "reCAPTCHA response JSON parsing failed: {Message}", ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "reCAPTCHA verification encountered unexpected error: {Message}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Models the response from Google's reCAPTCHA v2 verification API.
        /// </summary>
        private sealed class RecaptchaVerifyResponse
        {
            /// <summary>
            /// Whether this request was a valid reCAPTCHA token for your site.
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
