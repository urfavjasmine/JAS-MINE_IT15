using System.Text.Json;
using JAS_MINE_IT15.Data;
using JAS_MINE_IT15.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JAS_MINE_IT15.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PayMongoController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PayMongoController> _logger;

        public PayMongoController(ApplicationDbContext context, ILogger<PayMongoController> logger)
        {
            _context = context;
            _logger = logger;
        }

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
                var eventType = root.GetProperty("data").GetProperty("attributes").GetProperty("type").GetString();

                if (eventType == "checkout_session.paid")
                {
                    var data = root.GetProperty("data").GetProperty("attributes").GetProperty("data");
                    var attributes = data.GetProperty("attributes");
                    var referenceNumber = attributes.GetProperty("reference_number").GetString();
                    var amount = attributes.GetProperty("payment_intent").GetProperty("attributes").GetProperty("amount").GetInt64();
                    
                    // Convert back from centavos
                    decimal paidAmount = amount / 100m;

                    await ProcessSuccessfulPayment(referenceNumber, paidAmount);
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing PayMongo Webhook");
                return BadRequest();
            }
        }

        private async Task ProcessSuccessfulPayment(string? invoiceNumber, decimal amount)
        {
            if (string.IsNullOrEmpty(invoiceNumber)) return;

            var invoice = await _context.Invoices
                .Include(i => i.Subscription)
                .ThenInclude(s => s.Plan)
                .FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber && i.IsActive);

            if (invoice == null)
            {
                _logger.LogWarning("Invoice not found for webhook: {InvoiceNumber}", invoiceNumber);
                return;
            }

            if (invoice.Status == "Paid")
            {
                _logger.LogInformation("Invoice {InvoiceNumber} already marked as paid.", invoiceNumber);
                return;
            }

            // 1. Update Invoice
            invoice.Status = "Paid";
            invoice.UpdatedAt = DateTime.Now;

            // 2. Update Subscription
            if (invoice.Subscription != null)
            {
                invoice.Subscription.Status = "Active";
                invoice.Subscription.UpdatedAt = DateTime.Now;
                
                // Ensure dates are correct (starting from today if it was pending)
                invoice.Subscription.StartDate = DateTime.Today;
                var duration = invoice.Subscription.Plan?.DurationMonths ?? 1;
                invoice.Subscription.EndDate = DateTime.Today.AddMonths(duration);
            }

            // 3. Create SubscriptionPayment record
            var payment = new SubscriptionPayment
            {
                InvoiceId = invoice.Id,
                SubscriptionId = invoice.SubscriptionId,
                Amount = amount,
                PaymentDate = DateTime.Now,
                PaymentMethod = "PayMongo",
                ReferenceNumber = invoiceNumber,
                Status = "Paid",
                IsActive = true,
                CreatedAt = DateTime.Now
            };
            _context.SubscriptionPayments.Add(payment);

            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully processed payment for Invoice: {InvoiceNumber}, Amount: {Amount}", invoiceNumber, amount);
        }
    }
}
