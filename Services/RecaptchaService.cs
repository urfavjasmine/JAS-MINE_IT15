using JAS_MINE_IT15.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace JAS_MINE_IT15.Services
{
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

        public async Task<bool> VerifyTokenAsync(string token, string? remoteIp)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            if (string.IsNullOrWhiteSpace(_settings.SecretKey))
            {
                _logger.LogWarning("reCAPTCHA secret key is not configured.");
                return false;
            }

            var payload = new Dictionary<string, string>
            {
                ["secret"] = _settings.SecretKey,
                ["response"] = token
            };

            if (!string.IsNullOrWhiteSpace(remoteIp))
                payload["remoteip"] = remoteIp;

            using var content = new FormUrlEncodedContent(payload);
            using var response = await _httpClient.PostAsync(_settings.VerifyUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("reCAPTCHA verification HTTP failure: {StatusCode}", (int)response.StatusCode);
                return false;
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<RecaptchaVerifyResponse>(json);

            if (result == null)
                return false;

            if (!result.Success)
                _logger.LogWarning("reCAPTCHA verification failed: {ErrorCodes}", string.Join(",", result.ErrorCodes ?? new List<string>()));

            return result.Success;
        }

        private sealed class RecaptchaVerifyResponse
        {
            public bool Success { get; set; }
            public List<string>? ErrorCodes { get; set; }
        }
    }
}
