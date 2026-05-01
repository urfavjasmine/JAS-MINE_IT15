using JAS_MINE_IT15.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JAS_MINE_IT15.Services
{
    public class TurnstileService : ITurnstileService
    {
        private readonly HttpClient _httpClient;
        private readonly TurnstileSettings _settings;
        private readonly ILogger<TurnstileService> _logger;

        public TurnstileService(HttpClient httpClient, IOptions<TurnstileSettings> options, ILogger<TurnstileService> logger)
        {
            _httpClient = httpClient;
            _settings = options.Value;
            _logger = logger;
        }

        public async Task<bool> VerifyTokenAsync(string token, string? remoteIp)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("Turnstile verification failed: token is empty");
                return false;
            }

            if (string.IsNullOrWhiteSpace(_settings.SecretKey))
            {
                _logger.LogError("Turnstile secret key is not configured. Update appsettings.json with your Cloudflare secret key.");
                return false;
            }

            try
            {
                var payload = new Dictionary<string, string>
                {
                    ["secret"] = _settings.SecretKey,
                    ["response"] = token
                };

                if (!string.IsNullOrWhiteSpace(remoteIp))
                {
                    payload["remoteip"] = remoteIp;
                }

                using var content = new FormUrlEncodedContent(payload);
                using var response = await _httpClient.PostAsync(_settings.VerifyUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Turnstile API HTTP error: Status={StatusCode}", (int)response.StatusCode);
                    return false;
                }

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<TurnstileVerifyResponse>(json);

                if (result == null)
                {
                    _logger.LogWarning("Turnstile response deserialization failed. Raw response: {Response}", json);
                    return false;
                }

                if (!result.Success)
                {
                    var codes = string.Join(", ", result.ErrorCodes ?? new List<string>());
                    _logger.LogWarning("Turnstile verification failed. Error codes: {ErrorCodes}", string.IsNullOrWhiteSpace(codes) ? "unknown" : codes);
                }

                return result.Success;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Turnstile HTTP request failed: {Message}", ex.Message);
                return false;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Turnstile response JSON parsing failed: {Message}", ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Turnstile verification encountered unexpected error: {Message}", ex.Message);
                return false;
            }
        }

        private sealed class TurnstileVerifyResponse
        {
            [JsonPropertyName("success")]
            public bool Success { get; set; }

            [JsonPropertyName("challenge_ts")]
            public string? ChallengeTimestamp { get; set; }

            [JsonPropertyName("hostname")]
            public string? Hostname { get; set; }

            [JsonPropertyName("error-codes")]
            public List<string>? ErrorCodes { get; set; }
        }
    }
}
