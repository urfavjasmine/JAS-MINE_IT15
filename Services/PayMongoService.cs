using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using JAS_MINE_IT15.Models;

namespace JAS_MINE_IT15.Services
{
    public interface IPayMongoService
    {
        Task<string?> CreateCheckoutSessionAsync(decimal amount, string description, string successUrl, string cancelUrl, string referenceId, string? paymentMethod = null);
        Task<PaymentIntentResponse?> CreatePaymentIntentAsync(decimal amount, string description, string? statementDescriptor = null);
        Task<PaymentIntentResponse?> GetPaymentIntentAsync(string paymentIntentId);
        Task<PaymentIntentResponse?> AttachPaymentMethodAsync(string paymentIntentId, string paymentMethodId, string? returnUrl = null);
    }

    /// <summary>
    /// Response model for PayMongo Payment Intent
    /// </summary>
    public class PaymentIntentResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public long Amount { get; set; }
        public string Currency { get; set; } = "PHP";
        public string? ClientKey { get; set; }
        public string? NextAction { get; set; }
        public string? RedirectUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class PayMongoService : IPayMongoService
    {
        private readonly HttpClient _httpClient;
        private readonly PayMongoSettings _settings;
        private readonly ILogger<PayMongoService>? _logger;

        public PayMongoService(HttpClient httpClient, IOptions<PayMongoSettings> options, ILogger<PayMongoService>? logger = null)
        {
            _httpClient = httpClient;
            _settings = options.Value;
            _logger = logger;

            if (string.IsNullOrEmpty(_settings.SecretKey))
            {
                throw new InvalidOperationException("PayMongo:SecretKey is not configured. Set it in appsettings.json or environment variable PayMongo__SecretKey.");
            }
            
            var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes(_settings.SecretKey + ":"));
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authHeader);
            _httpClient.BaseAddress = new Uri("https://api.paymongo.com/v1/");
        }

        /// <summary>
        /// Creates a PayMongo Payment Intent for server-side payment processing.
        /// </summary>
        /// <param name="amount">Amount in PHP (will be converted to centavos)</param>
        /// <param name="description">Payment description</param>
        /// <param name="statementDescriptor">Optional statement descriptor (max 22 chars)</param>
        /// <returns>Payment intent response with client_key for frontend</returns>
        public async Task<PaymentIntentResponse?> CreatePaymentIntentAsync(decimal amount, string description, string? statementDescriptor = null)
        {
            var requestBody = new
            {
                data = new
                {
                    attributes = new
                    {
                        amount = (int)(amount * 100), // Convert to centavos
                        currency = "PHP",
                        description = description,
                        statement_descriptor = statementDescriptor?.Length > 22 ? statementDescriptor[..22] : statementDescriptor,
                        payment_method_allowed = new[] { "gcash", "paymaya", "card", "dob", "dob_ubp" },
                        payment_method_options = new
                        {
                            card = new { request_three_d_secure = "any" }
                        },
                        capture_type = "automatic"
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger?.LogInformation("Creating PayMongo Payment Intent for amount: {Amount}", amount);

            var response = await _httpClient.PostAsync("payment_intents", content);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(responseJson);
                var data = doc.RootElement.GetProperty("data");
                var attributes = data.GetProperty("attributes");

                return new PaymentIntentResponse
                {
                    Id = data.GetProperty("id").GetString() ?? "",
                    Status = attributes.GetProperty("status").GetString() ?? "",
                    Amount = attributes.GetProperty("amount").GetInt64(),
                    Currency = attributes.GetProperty("currency").GetString() ?? "PHP",
                    ClientKey = attributes.GetProperty("client_key").GetString(),
                    CreatedAt = DateTime.Now
                };
            }

            _logger?.LogError("PayMongo CreatePaymentIntent failed: {Response}", responseJson);
            return null;
        }

        /// <summary>
        /// Gets the current status of a payment intent.
        /// </summary>
        public async Task<PaymentIntentResponse?> GetPaymentIntentAsync(string paymentIntentId)
        {
            var response = await _httpClient.GetAsync($"payment_intents/{paymentIntentId}");
            var responseJson = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(responseJson);
                var data = doc.RootElement.GetProperty("data");
                var attributes = data.GetProperty("attributes");

                var result = new PaymentIntentResponse
                {
                    Id = data.GetProperty("id").GetString() ?? "",
                    Status = attributes.GetProperty("status").GetString() ?? "",
                    Amount = attributes.GetProperty("amount").GetInt64(),
                    Currency = attributes.GetProperty("currency").GetString() ?? "PHP",
                    ClientKey = attributes.TryGetProperty("client_key", out var ck) ? ck.GetString() : null,
                    CreatedAt = DateTime.Now
                };

                // Check for next_action (e.g., 3DS redirect)
                if (attributes.TryGetProperty("next_action", out var nextAction) && nextAction.ValueKind != JsonValueKind.Null)
                {
                    result.NextAction = nextAction.GetProperty("type").GetString();
                    if (nextAction.TryGetProperty("redirect", out var redirect))
                    {
                        result.RedirectUrl = redirect.GetProperty("url").GetString();
                    }
                }

                return result;
            }

            _logger?.LogError("PayMongo GetPaymentIntent failed: {Response}", responseJson);
            return null;
        }

        /// <summary>
        /// Attaches a payment method to a payment intent (for card payments requiring 3DS).
        /// </summary>
        public async Task<PaymentIntentResponse?> AttachPaymentMethodAsync(string paymentIntentId, string paymentMethodId, string? returnUrl = null)
        {
            var requestBody = new
            {
                data = new
                {
                    attributes = new
                    {
                        payment_method = paymentMethodId,
                        client_key = (string?)null, // Will be handled by frontend
                        return_url = returnUrl
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"payment_intents/{paymentIntentId}/attach", content);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(responseJson);
                var data = doc.RootElement.GetProperty("data");
                var attributes = data.GetProperty("attributes");

                var result = new PaymentIntentResponse
                {
                    Id = data.GetProperty("id").GetString() ?? "",
                    Status = attributes.GetProperty("status").GetString() ?? "",
                    Amount = attributes.GetProperty("amount").GetInt64(),
                    Currency = attributes.GetProperty("currency").GetString() ?? "PHP",
                    CreatedAt = DateTime.Now
                };

                if (attributes.TryGetProperty("next_action", out var nextAction) && nextAction.ValueKind != JsonValueKind.Null)
                {
                    result.NextAction = nextAction.GetProperty("type").GetString();
                    if (nextAction.TryGetProperty("redirect", out var redirect))
                    {
                        result.RedirectUrl = redirect.GetProperty("url").GetString();
                    }
                }

                return result;
            }

            _logger?.LogError("PayMongo AttachPaymentMethod failed: {Response}", responseJson);
            return null;
        }

        /// <summary>
        /// Creates a PayMongo Checkout Session for redirect-based payment.
        /// </summary>
        public async Task<string?> CreateCheckoutSessionAsync(decimal amount, string description, string successUrl, string cancelUrl, string referenceId, string? paymentMethod = null)
        {
            // Determine payment methods based on selection
            string[] paymentMethodTypes = paymentMethod switch
            {
                "gcash" => new[] { "gcash" },
                "card" => new[] { "card" },
                _ => new[] { "gcash", "paymaya", "card", "dob", "dob_ubp" }
            };

            var requestBody = new
            {
                data = new
                {
                    attributes = new
                    {
                        show_description = true,
                        show_line_items = true,
                        cancel_url = cancelUrl,
                        success_url = successUrl,
                        description = description,
                        line_items = new[]
                        {
                            new
                            {
                                amount = (int)(amount * 100),
                                currency = "PHP",
                                description = description,
                                name = description,
                                quantity = 1
                            }
                        },
                        payment_method_types = paymentMethodTypes,
                        reference_number = referenceId
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger?.LogInformation("Creating PayMongo Checkout Session for reference: {ReferenceId}", referenceId);

            var response = await _httpClient.PostAsync("checkout_sessions", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseJson);
                return doc.RootElement.GetProperty("data").GetProperty("attributes").GetProperty("checkout_url").GetString();
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger?.LogError("PayMongo CreateCheckoutSession failed: {Response}", errorContent);
            return null;
        }
    }
}
