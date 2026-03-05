using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace JAS_MINE_IT15.Services
{
    public interface IPayMongoService
    {
        Task<string?> CreateCheckoutSessionAsync(decimal amount, string description, string successUrl, string cancelUrl, string referenceId);
    }

    public class PayMongoService : IPayMongoService
    {
        private readonly HttpClient _httpClient;
        private readonly string _secretKey;

        public PayMongoService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _secretKey = configuration["PayMongo:SecretKey"] ?? throw new ArgumentNullException("PayMongo:SecretKey not found.");
            
            var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes(_secretKey + ":"));
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authHeader);
            _httpClient.BaseAddress = new Uri("https://api.paymongo.com/v1/");
        }

        public async Task<string?> CreateCheckoutSessionAsync(decimal amount, string description, string successUrl, string cancelUrl, string referenceId)
        {
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
                                amount = (int)(amount * 100), // PayMongo uses centavos
                                currency = "PHP",
                                description = description,
                                name = description,
                                quantity = 1
                            }
                        },
                        payment_method_types = new[] { "gcash", "paymaya", "card", "dob", "dob_ubp" },
                        reference_number = referenceId
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("checkout_sessions", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseJson);
                return doc.RootElement.GetProperty("data").GetProperty("attributes").GetProperty("checkout_url").GetString();
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            // Log error here in a real app
            return null;
        }
    }
}
