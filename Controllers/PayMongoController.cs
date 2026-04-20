using System.Text.Json;
using JAS_MINE_IT15.Data;
using JAS_MINE_IT15.Models;
using JAS_MINE_IT15.Models.Entities;
using JAS_MINE_IT15.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace JAS_MINE_IT15.Controllers
{
    [ApiController]
    [EnableRateLimiting("api")]
    [Route("api/[controller]")]
    public class PayMongoController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PayMongoController> _logger;
        private readonly IPayMongoService _payMongoService;
        private readonly PayMongoSettings _settings;

        public PayMongoController(
            ApplicationDbContext context, 
            ILogger<PayMongoController> logger,
            IPayMongoService payMongoService,
            IOptions<PayMongoSettings> options)
        {
            _context = context;
            _logger = logger;
            _payMongoService = payMongoService;
            _settings = options.Value;
        }

        /// <summary>
        /// Creates a PayMongo Payment Intent and returns the JSON response.
        /// POST /api/paymongo/create-payment-intent
        /// </summary>
        [HttpPost("create-payment-intent")]
        public async Task<IActionResult> CreatePaymentIntent([FromBody] CreatePaymentIntentRequest request)
        {
            if (request.Amount <= 0)
            {
                return BadRequest(new { success = false, message = "Amount must be greater than zero." });
            }

            if (string.IsNullOrWhiteSpace(request.Description))
            {
                return BadRequest(new { success = false, message = "Description is required." });
            }

            try
            {
                var result = await _payMongoService.CreatePaymentIntentAsync(
                    request.Amount,
                    request.Description,
                    request.StatementDescriptor
                );

                if (result == null)
                {
                    return StatusCode(500, new { success = false, message = "Failed to create payment intent." });
                }

                _logger.LogInformation("Payment Intent created: {PaymentIntentId}, Amount: {Amount}", result.Id, request.Amount);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        paymentIntentId = result.Id,
                        clientKey = result.ClientKey,
                        status = result.Status,
                        amount = result.Amount / 100m, // Convert back to PHP
                        currency = result.Currency
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment intent");
                return StatusCode(500, new { success = false, message = "An error occurred while creating the payment intent." });
            }
        }

        /// <summary>
        /// Gets the status of a Payment Intent.
        /// GET /api/paymongo/payment-intent/{id}
        /// </summary>
        [HttpGet("payment-intent/{id}")]
        public async Task<IActionResult> GetPaymentIntent(string id)
        {
            try
            {
                var result = await _payMongoService.GetPaymentIntentAsync(id);

                if (result == null)
                {
                    return NotFound(new { success = false, message = "Payment intent not found." });
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        paymentIntentId = result.Id,
                        status = result.Status,
                        amount = result.Amount / 100m,
                        currency = result.Currency,
                        nextAction = result.NextAction,
                        redirectUrl = result.RedirectUrl
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment intent {PaymentIntentId}", id);
                return StatusCode(500, new { success = false, message = "An error occurred." });
            }
        }

        /// <summary>
        /// Creates a PayMongo Checkout Session and returns the checkout URL.
        /// POST /api/paymongo/create-checkout
        /// </summary>
        [HttpPost("create-checkout")]
        public async Task<IActionResult> CreateCheckoutSession([FromBody] CreateCheckoutRequest request)
        {
            if (request.Amount <= 0)
            {
                return BadRequest(new { success = false, message = "Amount must be greater than zero." });
            }

            if (string.IsNullOrWhiteSpace(request.Description))
            {
                return BadRequest(new { success = false, message = "Description is required." });
            }

            try
            {
                // Build URLs
                var scheme = Request.Scheme;
                var host = Request.Host.Value;
                var successUrl = !string.IsNullOrEmpty(request.SuccessUrl) 
                    ? request.SuccessUrl 
                    : $"{scheme}://{host}/Home/PaymentSuccess";
                var cancelUrl = !string.IsNullOrEmpty(request.CancelUrl) 
                    ? request.CancelUrl 
                    : $"{scheme}://{host}/Home/PaymentCancel";

                var checkoutUrl = await _payMongoService.CreateCheckoutSessionAsync(
                    request.Amount,
                    request.Description,
                    successUrl,
                    cancelUrl,
                    request.ReferenceId ?? Guid.NewGuid().ToString("N")[..12].ToUpper(),
                    request.PaymentMethod
                );

                if (string.IsNullOrEmpty(checkoutUrl))
                {
                    return StatusCode(500, new { success = false, message = "Failed to create checkout session." });
                }

                _logger.LogInformation("Checkout session created for amount: {Amount}, reference: {Reference}", 
                    request.Amount, request.ReferenceId);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        checkoutUrl = checkoutUrl,
                        referenceId = request.ReferenceId,
                        amount = request.Amount
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating checkout session");
                return StatusCode(500, new { success = false, message = "An error occurred while creating the checkout session." });
            }
        }

        /// <summary>
        /// Initiates a payment for an invoice.
        /// POST /api/paymongo/pay-invoice
        /// </summary>
        [HttpPost("pay-invoice")]
        public async Task<IActionResult> PayInvoice([FromBody] PayInvoiceRequest request)
        {
            if (request.InvoiceId <= 0)
            {
                return BadRequest(new { success = false, message = "Invalid invoice ID." });
            }

            try
            {
                var invoice = await _context.Invoices
                    .Include(i => i.Subscription)
                    .ThenInclude(s => s!.Plan)
                    .FirstOrDefaultAsync(i => i.Id == request.InvoiceId && i.IsActive);

                if (invoice == null)
                {
                    return NotFound(new { success = false, message = "Invoice not found." });
                }

                if (invoice.Status == "Paid")
                {
                    return BadRequest(new { success = false, message = "Invoice is already paid." });
                }

                // Build URLs
                var scheme = Request.Scheme;
                var host = Request.Host.Value;
                var successUrl = !string.IsNullOrEmpty(request.SuccessUrl) 
                    ? request.SuccessUrl 
                    : $"{scheme}://{host}/Home/PaymentSuccess?invoiceId={invoice.Id}";
                var cancelUrl = !string.IsNullOrEmpty(request.CancelUrl) 
                    ? request.CancelUrl 
                    : $"{scheme}://{host}/Home/PaymentCancel?invoiceId={invoice.Id}";

                var description = $"JAS-MINE: {invoice.Subscription?.Plan?.Name ?? "Subscription"} - Invoice #{invoice.InvoiceNumber}";

                var checkoutUrl = await _payMongoService.CreateCheckoutSessionAsync(
                    invoice.Amount,
                    description,
                    successUrl,
                    cancelUrl,
                    invoice.InvoiceNumber,
                    request.PaymentMethod
                );

                if (string.IsNullOrEmpty(checkoutUrl))
                {
                    return StatusCode(500, new { success = false, message = "Failed to create checkout session." });
                }

                _logger.LogInformation("Payment initiated for Invoice: {InvoiceNumber}, Amount: {Amount}", 
                    invoice.InvoiceNumber, invoice.Amount);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        checkoutUrl = checkoutUrl,
                        invoiceNumber = invoice.InvoiceNumber,
                        amount = invoice.Amount,
                        planName = invoice.Subscription?.Plan?.Name
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating payment for invoice {InvoiceId}", request.InvoiceId);
                return StatusCode(500, new { success = false, message = "An error occurred while initiating payment." });
            }
        }

        /// <summary>
        /// Gets the public key for client-side PayMongo integration.
        /// GET /api/paymongo/public-key
        /// </summary>
        [HttpGet("public-key")]
        public IActionResult GetPublicKey()
        {
            if (string.IsNullOrEmpty(_settings.PublicKey))
            {
                return StatusCode(500, new { success = false, message = "PayMongo public key not configured." });
            }

            return Ok(new
            {
                success = true,
                publicKey = _settings.PublicKey
            });
        }

        /// <summary>
        /// PayMongo Webhook endpoint - listens for payment events.
        /// POST /api/paymongo/webhook
        /// </summary>
        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook()
        {
            using var reader = new StreamReader(Request.Body);
            var json = await reader.ReadToEndAsync();
            
            _logger.LogInformation("PayMongo Webhook received: {Json}", json);

            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var eventData = root.GetProperty("data");
                var eventAttributes = eventData.GetProperty("attributes");
                var eventType = eventAttributes.GetProperty("type").GetString();

                _logger.LogInformation("PayMongo Webhook event type: {EventType}", eventType);

                switch (eventType)
                {
                    case "checkout_session.payment.paid":
                    case "checkout_session.paid":
                        await HandleCheckoutSessionPaid(eventAttributes);
                        break;

                    case "payment.paid":
                        await HandlePaymentPaid(eventAttributes);
                        break;

                    case "payment_intent.succeeded":
                        await HandlePaymentIntentSucceeded(eventAttributes);
                        break;

                    case "payment.failed":
                    case "payment_intent.payment_failed":
                        await HandlePaymentFailed(eventAttributes);
                        break;

                    default:
                        _logger.LogInformation("Unhandled webhook event type: {EventType}", eventType);
                        break;
                }

                return Ok(new { received = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing PayMongo Webhook");
                return BadRequest(new { error = "Webhook processing failed" });
            }
        }

        private async Task HandleCheckoutSessionPaid(JsonElement eventAttributes)
        {
            var data = eventAttributes.GetProperty("data");
            var attributes = data.GetProperty("attributes");
            
            string? referenceNumber = null;
            if (attributes.TryGetProperty("reference_number", out var refNum))
            {
                referenceNumber = refNum.GetString();
            }

            long amount = 0;
            if (attributes.TryGetProperty("payment_intent", out var paymentIntent))
            {
                var piAttributes = paymentIntent.GetProperty("attributes");
                amount = piAttributes.GetProperty("amount").GetInt64();
            }
            else if (attributes.TryGetProperty("line_items", out var lineItems))
            {
                foreach (var item in lineItems.EnumerateArray())
                {
                    amount += item.GetProperty("amount").GetInt64() * item.GetProperty("quantity").GetInt32();
                }
            }

            decimal paidAmount = amount / 100m;
            await ProcessSuccessfulPayment(referenceNumber, paidAmount, "checkout_session");
        }

        private async Task HandlePaymentPaid(JsonElement eventAttributes)
        {
            var data = eventAttributes.GetProperty("data");
            var attributes = data.GetProperty("attributes");
            
            var amount = attributes.GetProperty("amount").GetInt64();
            string? referenceNumber = null;

            if (attributes.TryGetProperty("description", out var desc))
            {
                referenceNumber = desc.GetString();
            }

            decimal paidAmount = amount / 100m;
            await ProcessSuccessfulPayment(referenceNumber, paidAmount, "payment");
        }

        private async Task HandlePaymentIntentSucceeded(JsonElement eventAttributes)
        {
            var data = eventAttributes.GetProperty("data");
            var attributes = data.GetProperty("attributes");
            
            var amount = attributes.GetProperty("amount").GetInt64();
            string? referenceNumber = null;

            if (attributes.TryGetProperty("description", out var desc))
            {
                referenceNumber = desc.GetString();
            }

            decimal paidAmount = amount / 100m;
            await ProcessSuccessfulPayment(referenceNumber, paidAmount, "payment_intent");
        }

        private async Task HandlePaymentFailed(JsonElement eventAttributes)
        {
            var data = eventAttributes.GetProperty("data");
            var attributes = data.GetProperty("attributes");

            string? referenceNumber = null;
            if (attributes.TryGetProperty("description", out var desc))
            {
                referenceNumber = desc.GetString();
            }

            if (string.IsNullOrEmpty(referenceNumber)) return;

            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.InvoiceNumber == referenceNumber && i.IsActive);

            if (invoice != null && invoice.Status != "Paid")
            {
                invoice.Status = "Failed";
                invoice.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                _logger.LogWarning("Payment failed for Invoice: {InvoiceNumber}", referenceNumber);
            }
        }

        private async Task ProcessSuccessfulPayment(string? invoiceNumber, decimal amount, string source)
        {
            if (string.IsNullOrEmpty(invoiceNumber))
            {
                _logger.LogWarning("No reference number in {Source} webhook", source);
                return;
            }

            var invoice = await _context.Invoices
                .Include(i => i.Subscription)
                .ThenInclude(s => s!.Plan)
                .FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber && i.IsActive);

            if (invoice == null)
            {
                _logger.LogWarning("Invoice not found for webhook: {InvoiceNumber}", invoiceNumber);
                return;
            }

            // Skip if already processed (Paid or PendingVerification)
            if (invoice.Status == "Paid" || invoice.Status == "PendingVerification")
            {
                _logger.LogInformation("Invoice {InvoiceNumber} already processed (Status: {Status}).", invoiceNumber, invoice.Status);
                return;
            }

            // 1. Update Invoice to PendingVerification (NOT Paid - requires Super Admin approval)
            invoice.Status = "PendingVerification";
            invoice.UpdatedAt = DateTime.Now;

            // 2. DO NOT update Subscription status here - that happens on approval
            // Subscription remains in its current state until Super Admin approves

            // 3. Create SubscriptionPayment record with PendingVerification status
            var existingPayment = await _context.SubscriptionPayments
                .AnyAsync(p => p.ReferenceNumber == invoiceNumber && (p.Status == "Paid" || p.Status == "PendingVerification" || p.Status == "Approved"));

            if (!existingPayment)
            {
                var payment = new SubscriptionPayment
                {
                    InvoiceId = invoice.Id,
                    SubscriptionId = invoice.SubscriptionId,
                    Amount = amount,
                    PaymentDate = DateTime.Now,
                    PaymentMethod = "PayMongo",
                    ReferenceNumber = invoiceNumber,
                    Status = "PendingVerification",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };
                _context.SubscriptionPayments.Add(payment);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Payment received from {Source} for Invoice: {InvoiceNumber}, Amount: {Amount}. Pending verification.", 
                source, invoiceNumber, amount);
        }
    }

    /// <summary>
    /// Request model for creating a payment intent
    /// </summary>
    public class CreatePaymentIntentRequest
    {
        /// <summary>
        /// Amount in PHP
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Payment description
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Optional statement descriptor (max 22 characters)
        /// </summary>
        public string? StatementDescriptor { get; set; }

        /// <summary>
        /// Optional reference ID for tracking
        /// </summary>
        public string? ReferenceId { get; set; }
    }

    /// <summary>
    /// Request model for creating a checkout session
    /// </summary>
    public class CreateCheckoutRequest
    {
        /// <summary>
        /// Amount in PHP (e.g., 100.00 for 100 pesos)
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Description shown on the checkout page
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Optional reference ID for tracking
        /// </summary>
        public string? ReferenceId { get; set; }

        /// <summary>
        /// Optional custom success URL
        /// </summary>
        public string? SuccessUrl { get; set; }

        /// <summary>
        /// Optional custom cancel URL
        /// </summary>
        public string? CancelUrl { get; set; }

        /// <summary>
        /// Payment method type (e.g., "gcash", "card", "grab_pay", or null for all)
        /// </summary>
        public string? PaymentMethod { get; set; }
    }

    /// <summary>
    /// Request model for paying an invoice
    /// </summary>
    public class PayInvoiceRequest
    {
        /// <summary>
        /// Invoice ID to pay
        /// </summary>
        public int InvoiceId { get; set; }

        /// <summary>
        /// Optional custom success URL
        /// </summary>
        public string? SuccessUrl { get; set; }

        /// <summary>
        /// Optional custom cancel URL
        /// </summary>
        public string? CancelUrl { get; set; }

        /// <summary>
        /// Payment method type (e.g., "gcash", "card", "grab_pay", or null for all)
        /// </summary>
        public string? PaymentMethod { get; set; }
    }
}
