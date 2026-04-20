using JAS_MINE_IT15.Data;
using JAS_MINE_IT15.Filters;
using JAS_MINE_IT15.Models;
using JAS_MINE_IT15.Models.Entities;
using JAS_MINE_IT15.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace JAS_MINE_IT15.Controllers
{
    [Authorize]
    public class HomeController : BaseAppController
    {
        // ✅ Identity services (needed for DB login)
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private new readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly ILogger<HomeController> _logger;
        private readonly IPayMongoService _payMongoService;
        private readonly IEmailSender _emailSender;
        private readonly IRecaptchaService _recaptchaService;
        private readonly RecaptchaSettings _recaptchaSettings;
        private readonly RetentionSettings _retentionSettings;
        private const string LoginFailedAttemptsKey = "LoginFailedAttempts";
        private const string PendingRegistrationKey = "PendingRegistration";
        private const string PendingRegistrationCreatedAtKey = "PendingRegistrationCreatedAtTicks";

        public HomeController(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            ApplicationDbContext context,
            INotificationService notificationService,
            ILogger<HomeController> logger,
            IPayMongoService payMongoService,
            IEmailSender emailSender,
            IRecaptchaService recaptchaService,
            IOptions<RecaptchaSettings> recaptchaOptions,
            IOptions<RetentionSettings> retentionOptions)
            : base(context)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
            _payMongoService = payMongoService;
            _emailSender = emailSender;
            _recaptchaService = recaptchaService;
            _recaptchaSettings = recaptchaOptions.Value;
            _retentionSettings = retentionOptions.Value;
        }

        // Helper methods inherited from BaseAppController

        /// <summary>
        /// Checks if a barangay has at least one user with the 'barangay_admin' role.
        /// </summary>
        private async Task<bool> CheckBarangayAdminExistsAsync(int barangayId)
        {
            return await _context.BusinessUsers
                .AnyAsync(u => u.BarangayId == barangayId 
                    && u.Role == "barangay_admin" 
                    && u.IsActive);
        }

        private int GetLoginFailedAttempts()
        {
            var raw = HttpContext.Session.GetString(LoginFailedAttemptsKey);
            return int.TryParse(raw, out var value) ? value : 0;
        }

        private int IncrementLoginFailedAttempts()
        {
            var nextValue = GetLoginFailedAttempts() + 1;
            HttpContext.Session.SetString(LoginFailedAttemptsKey, nextValue.ToString());
            return nextValue;
        }

        private void ResetLoginFailedAttempts()
        {
            HttpContext.Session.Remove(LoginFailedAttemptsKey);
        }

        private int GetPendingRegistrationRetentionDays()
        {
            return Math.Max(1, _retentionSettings.PendingRegistrationRetentionDays);
        }

        private string? GetValidPendingRegistrationJson()
        {
            var pendingJson = HttpContext.Session.GetString(PendingRegistrationKey);
            if (string.IsNullOrWhiteSpace(pendingJson))
            {
                return null;
            }

            var createdTicksRaw = HttpContext.Session.GetString(PendingRegistrationCreatedAtKey);
            if (!long.TryParse(createdTicksRaw, out var createdTicks))
            {
                HttpContext.Session.Remove(PendingRegistrationKey);
                HttpContext.Session.Remove(PendingRegistrationCreatedAtKey);
                return null;
            }

            var createdAtUtc = new DateTime(createdTicks, DateTimeKind.Utc);
            if (DateTime.UtcNow - createdAtUtc > TimeSpan.FromDays(GetPendingRegistrationRetentionDays()))
            {
                HttpContext.Session.Remove(PendingRegistrationKey);
                HttpContext.Session.Remove(PendingRegistrationCreatedAtKey);
                _logger.LogInformation("Expired pending registration removed from session.");
                return null;
            }

            return pendingJson;
        }

        private void SetRecaptchaSiteKey()
        {
            ViewData["RecaptchaSiteKey"] = _recaptchaSettings.SiteKey;
        }

        private static string HashResetToken(string encodedToken)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(encodedToken));
            return Convert.ToHexString(bytes);
        }

        /// <summary>
        /// Validates if reCAPTCHA is properly configured with both keys.
        /// Checks for placeholder values to ensure real keys are configured.
        /// </summary>
        private bool IsCaptchaConfigured()
        {
            return !string.IsNullOrWhiteSpace(_recaptchaSettings.SiteKey)
                && _recaptchaSettings.SiteKey != "YOUR_RECAPTCHA_V3_SITE_KEY"
                && _recaptchaSettings.SiteKey != "YOUR_SITE_KEY_HERE"
                && !string.IsNullOrWhiteSpace(_recaptchaSettings.SecretKey)
                && _recaptchaSettings.SecretKey != "REPLACE_WITH_YOUR_REAL_SECRET_KEY"
                && _recaptchaSettings.SecretKey != "YOUR_SECRET_KEY_HERE";
        }

        /// <summary>
        /// Verifies a reCAPTCHA v2 token.
        /// </summary>
        private async Task<bool> IsRecaptchaValidAsync(string? token)
        {
            if (!IsCaptchaConfigured())
            {
                _logger.LogError("reCAPTCHA is not properly configured. " +
                    "Please ensure both SiteKey and SecretKey are set in appsettings.json. " +
                    "Security verification has been blocked.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("reCAPTCHA token is empty or null. Verification failed.");
                return false;
            }

            var remoteIp = HttpContext?.Connection?.RemoteIpAddress?.ToString();

            _logger.LogDebug("Attempting reCAPTCHA v2 verification. RemoteIP: {RemoteIp}", remoteIp ?? "unknown");

            var isValid = await _recaptchaService.VerifyTokenAsync(token, remoteIp);

            if (!isValid)
            {
                _logger.LogWarning("reCAPTCHA v2 verification failed. IP: {RemoteIp}", remoteIp ?? "unknown");
            }
            else
            {
                _logger.LogDebug("reCAPTCHA v2 verification succeeded. IP: {RemoteIp}", remoteIp ?? "unknown");
            }

            return isValid;
        }

        // GET: Home Index
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // If already logged in, go dashboard
            if (IsLoggedIn())
                return RedirectToDashboard();

            // Load plans from DB for the landing page
            ViewBag.Plans = await _context.SubscriptionPlans
                .Where(p => p.IsActive)
                .OrderBy(p => p.Price)
                .ToListAsync();

            return View("LandingPage");
        }

        // GET: /Home/LandingPage
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> LandingPage()
        {
            // Load plans from DB for the landing page
            ViewBag.Plans = await _context.SubscriptionPlans
                .Where(p => p.IsActive)
                .OrderBy(p => p.Price)
                .ToListAsync();

            return View();
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult PrivacyPolicy()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult TermsOfUse()
        {
            return View();
        }

        // GET: /Home/BarangaySubscriptions
        [HttpGet]
        public async Task<IActionResult> BarangaySubscriptions(string q = "", string status = "all")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToDashboard();

            q = (q ?? "").Trim();
            status = (status ?? "all").Trim();

            // Fetch subscriptions from database with related entities
            var allSubscriptions = await _context.BarangaySubscriptions
                .Where(s => s.IsActive)
                .Include(s => s.Barangay)
                .Include(s => s.Plan)
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new SubscriptionItem
                {
                    Id = s.Id.ToString(),
                    BarangayName = s.Barangay != null ? s.Barangay.Name : "",
                    PlanName = s.Plan != null ? s.Plan.Name : "",
                    StartDate = s.StartDate.ToString("yyyy-MM-dd"),
                    EndDate = s.EndDate.ToString("yyyy-MM-dd"),
                    Status = s.EndDate < DateTime.Today && s.Status != "Cancelled" ? "Expired" : s.Status
                })
                .ToListAsync();

            var filtered = allSubscriptions.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var qq = q.ToLower();
                filtered = filtered.Where(s =>
                    (s.BarangayName ?? "").ToLower().Contains(qq) ||
                    (s.PlanName ?? "").ToLower().Contains(qq)
                );
            }

            if (status != "all")
                filtered = filtered.Where(s => s.Status == status);

            var list = filtered.ToList();

            // Fetch barangays and plans from database
            var barangays = await _context.Barangays.Where(b => b.IsActive).OrderBy(b => b.Name).Select(b => b.Name).ToListAsync();
            var plans = await _context.SubscriptionPlans.Where(p => p.IsActive).OrderBy(p => p.Name).Select(p => p.Name).ToListAsync();

            var vm = new BarangaySubscriptionsViewModel
            {
                SearchQuery = q,
                StatusFilter = status,
                Subscriptions = list,

                TotalCount = allSubscriptions.Count,
                ActiveCount = allSubscriptions.Count(x => x.Status == "Active"),
                PendingCount = allSubscriptions.Count(x => x.Status == "Pending"),
                ExpiredCount = allSubscriptions.Count(x => x.Status == "Expired"),
                CancelledCount = allSubscriptions.Count(x => x.Status == "Cancelled"),

                Barangays = barangays,
                Plans = plans,

                SuccessMessage = TempData["Success"] as string,
                ErrorMessage = TempData["Error"] as string
            };

            return View(vm);
        }

        // POST: Create (Assign Plan)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
        public async Task<IActionResult> CreateSubscription(string barangayName, string planName, string startDate, string endDate, string q = "", string status = "all")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToDashboard();

            barangayName = (barangayName ?? "").Trim();
            planName = (planName ?? "").Trim();
            startDate = (startDate ?? "").Trim();
            endDate = (endDate ?? "").Trim();

            if (string.IsNullOrWhiteSpace(barangayName) || string.IsNullOrWhiteSpace(planName) ||
                string.IsNullOrWhiteSpace(startDate) || string.IsNullOrWhiteSpace(endDate))
            {
                TempData["Error"] = "Please complete all fields.";
                return RedirectToAction(nameof(BarangaySubscriptions), new { q, status });
            }

            // Find barangay and plan by name
            var bgy = await _context.Barangays.FirstOrDefaultAsync(b => b.Name == barangayName.Trim() && b.IsActive);
            var plan = await _context.SubscriptionPlans.FirstOrDefaultAsync(p => p.Name == planName.Trim() && p.IsActive);

            if (bgy == null || plan == null)
            {
                TempData["Error"] = "Invalid barangay or plan selected.";
                return RedirectToAction(nameof(BarangaySubscriptions), new { q, status });
            }

            if (!DateTime.TryParse(startDate, out var parsedStart) || !DateTime.TryParse(endDate, out var parsedEnd))
            {
                TempData["Error"] = "Invalid date format.";
                return RedirectToAction(nameof(BarangaySubscriptions), new { q, status });
            }

            var subscription = new BarangaySubscription
            {
                BarangayId = bgy.Id,
                PlanId = plan.Id,
                StartDate = parsedStart,
                EndDate = parsedEnd,
                Status = parsedEnd >= DateTime.Today ? "Active" : "Expired",
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.BarangaySubscriptions.Add(subscription);
            await _context.SaveChangesAsync();
            await LogAuditAsync("Create", "BarangaySubscriptions", subscription.Id, "Subscription", $"{barangayName} - {planName}", $"Assigned {planName} to {barangayName}");

            TempData["Success"] = $"{planName} assigned to {barangayName}.";
            return RedirectToAction(nameof(BarangaySubscriptions), new { q, status });
        }

        // POST: Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
        public async Task<IActionResult> EditSubscription(string id, string barangayName, string planName, string startDate, string endDate, string q = "", string status = "all")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToDashboard();

            if (!int.TryParse(id, out var subscriptionId))
            {
                TempData["Error"] = "Invalid subscription ID.";
                return RedirectToAction(nameof(BarangaySubscriptions), new { q, status });
            }

            var subscription = await _context.BarangaySubscriptions
                .Include(s => s.Barangay)
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s => s.Id == subscriptionId);

            if (subscription == null)
            {
                TempData["Error"] = "Subscription not found.";
                return RedirectToAction(nameof(BarangaySubscriptions), new { q, status });
            }

            // Update barangay if name changed
            if (!string.IsNullOrWhiteSpace(barangayName))
            {
                var barangay = await _context.Barangays.FirstOrDefaultAsync(b => b.Name == barangayName.Trim());
                if (barangay != null) subscription.BarangayId = barangay.Id;
            }

            // Update plan if name changed
            if (!string.IsNullOrWhiteSpace(planName))
            {
                var plan = await _context.SubscriptionPlans.FirstOrDefaultAsync(p => p.Name == planName.Trim());
                if (plan != null) subscription.PlanId = plan.Id;
            }

            // Update dates
            if (DateTime.TryParse(startDate, out var start)) subscription.StartDate = start;
            if (DateTime.TryParse(endDate, out var end)) subscription.EndDate = end;

            // Recompute status if not cancelled
            if (subscription.Status != "Cancelled")
            {
                subscription.Status = subscription.EndDate < DateTime.Today ? "Expired" : "Active";
            }

            subscription.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Subscription updated successfully.";
            return RedirectToAction(nameof(BarangaySubscriptions), new { q, status });
        }

        // POST: Cancel
        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
        public async Task<IActionResult> CancelSubscription(string id, string q = "", string status = "all")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToDashboard();

            if (int.TryParse(id, out var subscriptionId))
            {
                var subscription = await _context.BarangaySubscriptions
                    .Include(s => s.Barangay)
                    .Include(s => s.Plan)
                    .FirstOrDefaultAsync(s => s.Id == subscriptionId);
                if (subscription != null)
                {
                    subscription.Status = "Cancelled";
                    subscription.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                    var label = $"{subscription.Barangay?.Name} - {subscription.Plan?.Name}";
                    await LogAuditAsync("Cancel", "BarangaySubscriptions", subscription.Id, "Subscription", label, $"Cancelled subscription: {label}");
                }
            }

            TempData["Success"] = "Subscription cancelled.";
            return RedirectToAction(nameof(BarangaySubscriptions), new { q, status });
        }

        // POST: Archive Subscription
        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
        public async Task<IActionResult> ArchiveSubscription(string id, string q = "", string status = "all")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToDashboard();

            if (int.TryParse(id, out var subscriptionId))
            {
                var subscription = await _context.BarangaySubscriptions
                    .Include(s => s.Barangay)
                    .Include(s => s.Plan)
                    .FirstOrDefaultAsync(s => s.Id == subscriptionId);
                if (subscription != null)
                {
                    subscription.IsActive = false;
                    subscription.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                    var label = $"{subscription.Barangay?.Name} - {subscription.Plan?.Name}";
                    await LogAuditAsync("Archived", "BarangaySubscriptions", subscription.Id, "Subscription", label, $"Archived subscription: {label}");
                }
            }

            TempData["Success"] = "Subscription archived.";
            return RedirectToAction(nameof(BarangaySubscriptions), new { q, status });
        }

        // GET: /Home/MySubscription
        [HttpGet]
        public async Task<IActionResult> MySubscription(bool expired = false)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            // If subscription is expired and role is not admin, show dedicated expired page
            if (expired && !IsSuperAdmin())
            {
                var bgId = GetCurrentBarangayId();
                if (bgId.HasValue)
                {
                    var hasActive = await _context.BarangaySubscriptions
                        .AnyAsync(s => s.BarangayId == bgId && s.IsActive && s.Status == "Active" && s.EndDate >= DateTime.Today);
                    if (!hasActive)
                        return View("SubscriptionExpired");
                }
            }

            var barangayId = GetCurrentBarangayId();
            var barangayName = HttpContext.Session.GetString("Barangay") ?? "Your Barangay";

            // Find active/pending subscription for this barangay
            var subscription = await _context.BarangaySubscriptions
                .Where(s => s.IsActive && s.BarangayId == barangayId)
                .Include(s => s.Plan)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync();

            // Get payment history
            var payments = new List<MySubscriptionViewModel.PaymentRow>();
            if (subscription != null)
            {
                payments = await _context.SubscriptionPayments
                    .Where(p => p.IsActive && p.SubscriptionId == subscription.Id)
                    .OrderByDescending(p => p.PaymentDate)
                    .Select(p => new MySubscriptionViewModel.PaymentRow
                    {
                        Id = p.Id.ToString(),
                        Date = p.PaymentDate.ToString("yyyy-MM-dd"),
                        Amount = p.Amount,
                        Method = p.PaymentMethod ?? "Cash",
                        Status = p.Status,
                        Reference = p.ReferenceNumber ?? "",
                        ProofUrl = p.ProofOfPaymentUrl,
                        RejectionReason = p.RejectionReason
                    })
                    .ToListAsync();
            }

            // Get invoices for this barangay
            var invoices = await _context.Invoices
                .Where(i => i.IsActive && i.BarangayId == barangayId)
                .OrderByDescending(i => i.IssuedAt)
                .Select(i => new MySubscriptionViewModel.InvoiceRow
                {
                    Id = i.Id,
                    InvoiceNumber = i.InvoiceNumber,
                    Amount = i.Amount,
                    Status = i.Status,
                    IssuedAt = i.IssuedAt.ToString("yyyy-MM-dd"),
                    DueDate = i.DueDate.HasValue ? i.DueDate.Value.ToString("yyyy-MM-dd") : null
                })
                .ToListAsync();

            // Check if subscription can be cancelled (Pending and has no approved/pending verification payments)
            bool canCancel = false;
            if (subscription != null && subscription.Status == "Pending")
            {
                // Can cancel if there are no payments at all, or all payments are Rejected
                var hasBlockingPayment = await _context.SubscriptionPayments
                    .AnyAsync(p => p.SubscriptionId == subscription.Id && p.IsActive && 
                        (p.Status == "Paid" || p.Status == "Approved" || p.Status == "PendingVerification"));
                canCancel = !hasBlockingPayment;
            }

            var vm = new MySubscriptionViewModel
            {
                BarangayName = barangayName,
                ShowExpiredWarning = expired,
                SuccessMessage = TempData["Success"] as string,
                ErrorMessage = TempData["Error"] as string,
                Subscription = subscription != null ? new MySubscriptionViewModel.SubscriptionSummary
                {
                    SubscriptionId = subscription.Id,
                    PlanName = subscription.Plan?.Name ?? "Unknown Plan",
                    Price = subscription.Plan?.Price ?? 0m,
                    Status = subscription.EndDate < DateTime.Today && subscription.Status != "Cancelled" ? "Expired" : subscription.Status,
                    StartDate = subscription.StartDate.ToString("yyyy-MM-dd"),
                    EndDate = subscription.EndDate.ToString("yyyy-MM-dd"),
                    CanCancel = canCancel
                } : new MySubscriptionViewModel.SubscriptionSummary
                {
                    SubscriptionId = 0,
                    PlanName = "No Active Plan",
                    Price = 0m,
                    Status = "None",
                    StartDate = "",
                    EndDate = "",
                    CanCancel = false
                },
                Payments = payments,
                Invoices = invoices
            };

            return View(vm);
        }

        // GET: /Home/SubscriptionPayments
        [HttpGet]
        public async Task<IActionResult> SubscriptionPayments(string q = "", string status = "")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToDashboard();

            q = (q ?? "").Trim();
            status = (status ?? "").Trim();

            var allPayments = await _context.SubscriptionPayments
                .Where(p => p.IsActive)
                .Include(p => p.Subscription)
                    .ThenInclude(s => s!.Barangay)
                .Include(p => p.Subscription)
                    .ThenInclude(s => s!.Plan)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new PaymentItem
                {
                    Id = p.Id.ToString(),
                    BarangayName = p.Subscription != null && p.Subscription.Barangay != null ? p.Subscription.Barangay.Name : "",
                    PlanName = p.Subscription != null && p.Subscription.Plan != null ? p.Subscription.Plan.Name : "",
                    Amount = p.Amount,
                    PaymentDate = p.PaymentDate.ToString("yyyy-MM-dd"),
                    PaymentMethod = p.PaymentMethod ?? "Cash",
                    Status = p.Status,
                    Reference = p.ReferenceNumber ?? ""
                })
                .ToListAsync();

            var list = allPayments.AsEnumerable();

            // Filter by search query
            if (!string.IsNullOrWhiteSpace(q))
            {
                var qq = q.ToLower();
                list = list.Where(p =>
                    (p.BarangayName ?? "").ToLower().Contains(qq) ||
                    (p.PlanName ?? "").ToLower().Contains(qq)
                );
            }

            // Filter by status
            if (!string.IsNullOrWhiteSpace(status))
            {
                list = list.Where(p => p.Status == status);
            }

            var filtered = list.ToList();

            var totalPaid = allPayments.Where(p => p.Status == "Paid" || p.Status == "Approved").Sum(p => p.Amount);

            var barangays = await _context.Barangays.Where(b => b.IsActive).OrderBy(b => b.Name).Select(b => b.Name).ToListAsync();
            var plans = await _context.SubscriptionPlans.Where(p => p.IsActive).OrderBy(p => p.Name).Select(p => p.Name).ToListAsync();
            var methods = new List<string> { "Cash", "Bank Transfer", "GCash", "Maya", "Check" };

            var vm = new SubscriptionPaymentsViewModel
            {
                SearchQuery = q,
                StatusFilter = status,
                Payments = filtered,

                TotalPayments = allPayments.Count,
                TotalCollected = totalPaid,
                PendingCount = allPayments.Count(p => p.Status == "Pending"),
                PendingVerificationCount = allPayments.Count(p => p.Status == "PendingVerification"),
                ApprovedCount = allPayments.Count(p => p.Status == "Approved" || p.Status == "Paid"),
                RejectedCount = allPayments.Count(p => p.Status == "Rejected"),
                FailedCount = allPayments.Count(p => p.Status == "Failed"),

                Barangays = barangays,
                Plans = plans,
                Methods = methods,

                SuccessMessage = TempData["Success"] as string,
                ErrorMessage = TempData["Error"] as string
            };

            return View(vm);
        }

        // POST: Create (Record Payment)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
        public async Task<IActionResult> CreatePayment(string barangayName, string planName, decimal amount, string paymentDate, string paymentMethod, string status, string q = "")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToDashboard();

            barangayName = (barangayName ?? "").Trim();
            planName = (planName ?? "").Trim();
            paymentDate = string.IsNullOrWhiteSpace(paymentDate) ? DateTime.Now.ToString("yyyy-MM-dd") : paymentDate.Trim();
            paymentMethod = string.IsNullOrWhiteSpace(paymentMethod) ? "Cash" : paymentMethod.Trim();
            status = string.IsNullOrWhiteSpace(status) ? "Paid" : status.Trim();

            if (string.IsNullOrWhiteSpace(barangayName) || string.IsNullOrWhiteSpace(planName) || amount <= 0)
            {
                TempData["Error"] = "Please complete required fields (Barangay, Plan, Amount).";
                return RedirectToAction(nameof(SubscriptionPayments), new { q });
            }

            // Find the subscription matching barangay + plan
            var subscription = await _context.BarangaySubscriptions
                .Include(s => s.Barangay)
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s => s.IsActive && s.Barangay!.Name == barangayName.Trim() && s.Plan!.Name == planName.Trim());

            if (subscription == null)
            {
                TempData["Error"] = "No active subscription found for this barangay and plan.";
                return RedirectToAction(nameof(SubscriptionPayments), new { q });
            }

            var payment = new SubscriptionPayment
            {
                SubscriptionId = subscription.Id,
                Amount = amount,
                PaymentDate = DateTime.TryParse(paymentDate, out var pd) ? pd : DateTime.Now,
                PaymentMethod = paymentMethod,
                Status = status,
                ProcessedById = GetCurrentUserId(),
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.SubscriptionPayments.Add(payment);
            await _context.SaveChangesAsync();
            await LogAuditAsync("Create", "SubscriptionPayments", payment.Id, "Payment", $"{barangayName} - ₱{amount:N0}", $"Recorded payment of ₱{amount:N0} for {barangayName}");

            TempData["Success"] = $"Payment of ₱{amount:N0} recorded.";
            return RedirectToAction(nameof(SubscriptionPayments), new { q });
        }

        // POST: Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
        public async Task<IActionResult> EditPayment(string id, string barangayName, string planName, decimal amount, string paymentDate, string paymentMethod, string status, string q = "")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToDashboard();

            if (!int.TryParse(id, out var paymentId))
            {
                TempData["Error"] = "Invalid payment ID.";
                return RedirectToAction(nameof(SubscriptionPayments), new { q });
            }

            var payment = await _context.SubscriptionPayments
                .Include(p => p.Subscription)
                    .ThenInclude(s => s!.Barangay)
                .Include(p => p.Subscription)
                    .ThenInclude(s => s!.Plan)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment == null)
            {
                TempData["Error"] = "Payment record not found.";
                return RedirectToAction(nameof(SubscriptionPayments), new { q });
            }

            // Update subscription if barangay/plan changed
            if (!string.IsNullOrWhiteSpace(barangayName) && !string.IsNullOrWhiteSpace(planName))
            {
                var subscription = await _context.BarangaySubscriptions
                    .Include(s => s.Barangay)
                    .Include(s => s.Plan)
                    .FirstOrDefaultAsync(s => s.Barangay!.Name == barangayName.Trim() && s.Plan!.Name == planName.Trim());
                if (subscription != null) payment.SubscriptionId = subscription.Id;
            }

            if (amount > 0) payment.Amount = amount;
            if (DateTime.TryParse(paymentDate, out var date)) payment.PaymentDate = date;
            if (!string.IsNullOrWhiteSpace(paymentMethod)) payment.PaymentMethod = paymentMethod.Trim();
            if (!string.IsNullOrWhiteSpace(status)) payment.Status = status.Trim();

            payment.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Payment record updated successfully.";
            return RedirectToAction(nameof(SubscriptionPayments), new { q });
        }

        // POST: Archive
        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
        [Authorize(Roles = "super_admin,barangay_admin")]
        public async Task<IActionResult> ArchivePayment(string id, string q = "")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToDashboard();

            if (int.TryParse(id, out var paymentId))
            {
                var payment = await _context.SubscriptionPayments.FindAsync(paymentId);
                if (payment != null)
                {
                    payment.IsActive = false;
                    await _context.SaveChangesAsync();
                }
            }

            TempData["Success"] = "Payment archived.";
            return RedirectToAction(nameof(SubscriptionPayments), new { q });
        }

        // GET: /Home/SubscriptionPlans
        [HttpGet]
        public async Task<IActionResult> SubscriptionPlans(string q = "")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToDashboard();

            q = (q ?? "").Trim();

            var allPlans = await _context.SubscriptionPlans
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new PlanItem
                {
                    Id = p.Id.ToString(),
                    Name = p.Name,
                    Description = p.Description ?? "",
                    Price = p.Price,
                    DurationMonths = p.DurationMonths,
                    UserLimit = p.UserLimit,
                    Features = p.Features ?? "",
                    IsActive = p.IsActive
                })
                .ToListAsync();

            var list = allPlans.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var qq = q.ToLower();
                list = list.Where(p => (p.Name ?? "").ToLower().Contains(qq));
            }

            var filtered = list.ToList();

            var vm = new SubscriptionPlansViewModel
            {
                SearchQuery = q,
                Plans = filtered,

                TotalPlans = allPlans.Count,
                ActivePlans = allPlans.Count(p => p.IsActive),
                InactivePlans = allPlans.Count(p => !p.IsActive),
                YearlyPlans = allPlans.Count(p => p.DurationMonths >= 12),

                SuccessMessage = TempData["Success"] as string,
                ErrorMessage = TempData["Error"] as string
            };

            return View(vm);
        }

        // POST: Create Plan
        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
        public async Task<IActionResult> CreatePlan(string name, decimal price, int durationMonths, string description, bool isActive, string q = "")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToDashboard();

            name = (name ?? "").Trim();
            description = (description ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "Plan name is required.";
                return RedirectToAction(nameof(SubscriptionPlans), new { q });
            }

            if (durationMonths <= 0) durationMonths = 1;
            if (price < 0) price = 0;

            var plan = new SubscriptionPlan
            {
                Name = name,
                Description = description,
                Price = price,
                DurationMonths = durationMonths,
                IsActive = isActive,
                CreatedAt = DateTime.Now
            };

            _context.SubscriptionPlans.Add(plan);
            await _context.SaveChangesAsync();
            await LogAuditAsync("Create", "SubscriptionPlans", plan.Id, "Plan", name, $"Created plan: {name} (₱{price:N0}/{durationMonths}mo)");

            TempData["Success"] = $"\"{name}\" has been added.";
            return RedirectToAction(nameof(SubscriptionPlans), new { q });
        }

        // POST: Edit Plan
        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
        public async Task<IActionResult> EditPlan(string id, string name, decimal price, int durationMonths, string description, string isActive, string q = "")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToDashboard();

            if (!int.TryParse(id, out var planId))
            {
                TempData["Error"] = "Invalid plan ID.";
                return RedirectToAction(nameof(SubscriptionPlans), new { q });
            }

            var plan = await _context.SubscriptionPlans.FindAsync(planId);

            if (plan == null)
            {
                TempData["Error"] = "Plan not found.";
                return RedirectToAction(nameof(SubscriptionPlans), new { q });
            }

            name = (name ?? "").Trim();
            description = (description ?? "").Trim();

            if (!string.IsNullOrWhiteSpace(name)) plan.Name = name;
            if (price >= 0) plan.Price = price;
            if (durationMonths > 0) plan.DurationMonths = durationMonths;
            plan.Description = description;
            plan.IsActive = isActive == "true";
            plan.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"\"{plan.Name}\" has been updated.";
            return RedirectToAction(nameof(SubscriptionPlans), new { q });
        }

        // POST: Archive Plan
        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
        [Authorize(Roles = "super_admin,barangay_admin")]
        public async Task<IActionResult> ArchivePlan(string id, string q = "")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToDashboard();

            if (int.TryParse(id, out var planId))
            {
                var plan = await _context.SubscriptionPlans.FindAsync(planId);
                if (plan != null)
                {
                    plan.IsActive = false;
                    plan.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                }
            }

            TempData["Success"] = "Plan archived.";
            return RedirectToAction(nameof(SubscriptionPlans), new { q });
        }

        // GET: /Home/Register
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Register()
        {
            if (IsLoggedIn()) return RedirectToDashboard();
            SetRecaptchaSiteKey();
            return View(new RegisterViewModel());
        }

        // POST: /Home/Register
        // UPDATED: Do NOT create user yet - store data in session and proceed to plan selection
        // User will be created only after successful payment
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            // Log validation errors for debugging
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                _logger.LogWarning("Registration validation failed: {Errors}", string.Join(", ", errors));
                
                // Keep user on review step when there are validation errors
                model.CurrentStep = 3;
                SetRecaptchaSiteKey();
                return View(model);
            }

            model.Email = (model.Email ?? "").Trim().ToLower();
            model.BarangayName = (model.BarangayName ?? "").Trim();

            // 1. Check if user already exists in Identity
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                model.ErrorMessage = "A user with this email already exists.";
                model.CurrentStep = 3;
                SetRecaptchaSiteKey();
                return View(model);
            }

            // 2. Check if barangay name already exists
            var existingBarangay = await _context.Barangays
                .AnyAsync(b => b.Name.ToLower() == model.BarangayName.ToLower() && b.IsActive);
            if (existingBarangay)
            {
                model.ErrorMessage = "A barangay with this name already exists.";
                model.CurrentStep = 3;
                SetRecaptchaSiteKey();
                return View(model);
            }

            // 3. Store registration data in session (DO NOT create user yet)
            // User will be created after successful payment
            var pendingReg = new
            {
                model.FirstName,
                model.LastName,
                model.Email,
                model.Password,
                model.PhoneNumber,
                model.BarangayName,
                model.Municipality,
                model.Province,
                model.Region,
                model.Address,
                CreatedAt = DateTime.Now
            };
            HttpContext.Session.SetString(PendingRegistrationKey, System.Text.Json.JsonSerializer.Serialize(pendingReg));
            HttpContext.Session.SetString(PendingRegistrationCreatedAtKey, DateTime.UtcNow.Ticks.ToString());

            _logger.LogInformation("Pending registration stored for: {Email}, Barangay: {Barangay}", model.Email, model.BarangayName);

            // 4. Redirect to SelectPlan (user not created yet)
            TempData["Success"] = "Please select a subscription plan to complete your registration.";
            return RedirectToAction(nameof(SelectPlan));
        }

        // GET: /Home/Login
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(int? planId = null)
        {
            // If already logged in and a plan was selected, go to SelectPlan
            if (User.Identity?.IsAuthenticated == true)
            {
                if (planId.HasValue && GetCurrentRole() == "barangay_admin")
                    return RedirectToAction(nameof(SelectPlan), new { planId = planId.Value });
                return RedirectToDashboard();
            }

            // Store selected plan ID so it survives login round-trip
            if (planId.HasValue)
                TempData["SelectedPlanId"] = planId.Value;

            SetRecaptchaSiteKey();
            return View(new LoginViewModel
            {
                CaptchaRequired = GetLoginFailedAttempts() >= 3
            });
        }

        // ✅ POST /Home/Login (Identity DB login with reCAPTCHA validation)
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            var failedAttempts = GetLoginFailedAttempts();
            var captchaRequired = failedAttempts >= 3;

            // Validate model state
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Login attempt failed - invalid model state for email: {Email}", model.Email ?? "unknown");
                model.ErrorMessage = "Please fill up all required fields.";
                model.CaptchaRequired = captchaRequired;
                SetRecaptchaSiteKey();
                return View(model);
            }

            // 🔒 Validate reCAPTCHA if required (after 3 failed attempts)
            if (captchaRequired)
            {
                _logger.LogInformation("reCAPTCHA validation required - {FailedAttempts} failed attempts. Email: {Email}",
                    failedAttempts, model.Email ?? "unknown");

                if (!await IsRecaptchaValidAsync(model.RecaptchaToken))
                {
                    failedAttempts = IncrementLoginFailedAttempts();
                    _logger.LogWarning("Login blocked due to invalid CAPTCHA. " +
                        "Failed attempts: {FailedAttempts}, Email: {Email}", failedAttempts, model.Email ?? "unknown");
                    
                    model.ErrorMessage = "CAPTCHA verification failed. Please complete the security check and try again.";
                    model.CaptchaRequired = true;
                    SetRecaptchaSiteKey();
                    return View(model);
                }

                _logger.LogInformation("reCAPTCHA validation succeeded for email: {Email}", model.Email ?? "unknown");
            }

            // Prepare email and password
            model.Email = (model.Email ?? "").Trim();
            model.Password = (model.Password ?? "").Trim();

            _logger.LogInformation("Login attempt for email: {Email}", model.Email);

            // Find user by email
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                _logger.LogWarning("Failed login - user not found: {Email}", model.Email);
                failedAttempts = IncrementLoginFailedAttempts();
                model.ErrorMessage = "Invalid email or password.";
                model.CaptchaRequired = failedAttempts >= 3;
                SetRecaptchaSiteKey();
                return View(model);
            }

            _logger.LogDebug("User found: {Email}, attempting password sign-in", user.Email);

            HttpContext.Session.Clear();
            Response.Cookies.Delete(".AspNetCore.Session");
            await _signInManager.SignOutAsync();

            // Attempt password sign-in
            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!,
                model.Password,
                isPersistent: false,
                lockoutOnFailure: true
            );

            if (!result.Succeeded)
            {
                _logger.LogWarning("Failed login attempt - invalid password or locked out. " +
                    "Email: {Email}, IsLockedOut: {IsLockedOut}", model.Email, result.IsLockedOut);
                
                failedAttempts = IncrementLoginFailedAttempts();
                model.ErrorMessage = "Invalid email or password.";
                model.CaptchaRequired = failedAttempts >= 3;
                SetRecaptchaSiteKey();
                return View(model);
            }

            // Login succeeded - continue with role and business user processing
            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "";

            // Load BarangayId from BusinessUsers table (check ALL users, not just active)
            var businessUser = await _context.BusinessUsers
                .FirstOrDefaultAsync(u => u.Email.ToLower() == model.Email.ToLower());

            if (businessUser == null || !businessUser.IsActive)
            {
                _logger.LogWarning("Login denied for {Email}: no active business profile found", model.Email);
                await _signInManager.SignOutAsync();
                failedAttempts = IncrementLoginFailedAttempts();
                model.ErrorMessage = "Invalid email or password.";
                model.CaptchaRequired = failedAttempts >= 3;
                SetRecaptchaSiteKey();
                return View(model);
            }

            int? barangayId = businessUser.BarangayId;

            // Update last login timestamp
            businessUser.LastLoginAt = DateTime.Now;
            await _context.SaveChangesAsync();

            // Add BarangayId as claim
            await AddBarangayClaimAsync(user, barangayId);

            // ── Session fixation prevention: clear old session before setting new values ──
            HttpContext.Session.Clear();
            Response.Cookies.Delete(".AspNetCore.Session");
            ResetLoginFailedAttempts();

            // Keep session minimal for security.
            HttpContext.Session.SetString("UserId", businessUser?.Id.ToString() ?? "");
            HttpContext.Session.SetString("Role", role);

            _logger.LogInformation("Login successful for {Email}, Role={Role}, BarangayId={BarangayId}", model.Email, role, barangayId);

            await LogAuditAsync("Login", "Authentication", businessUser?.Id, "User", user.Email, $"User logged in: {user.Email}");

            // Redirect based on role
            switch (role)
            {
                case "super_admin":
                    return RedirectToAction("System", "Dashboard");

                case "barangay_admin":
                    // If a plan was selected before login, check if they actually need to subscribe
                    if (TempData["SelectedPlanId"] is int selectedPlanId)
                    {
                        var hasValidSub = await _context.BarangaySubscriptions
                            .AnyAsync(s => s.BarangayId == barangayId && s.IsActive && (s.Status == "Active" || s.Status == "Pending") && s.EndDate >= DateTime.Today);

                        if (!hasValidSub)
                            return RedirectToAction(nameof(SelectPlan), new { planId = selectedPlanId });
                    }
                    return RedirectToAction("Barangay", "Dashboard");

                case "user":
                    return RedirectToAction("Index", "Home");

                default:
                    return RedirectToAction("Barangay", "Dashboard");
            }
        }

        /// <summary>
        /// Adds BarangayId as a claim to the user. Removes existing claim first if present.
        /// </summary>
        private async Task AddBarangayClaimAsync(IdentityUser user, int? barangayId)
        {
            const string claimType = "BarangayId";

            // Remove existing BarangayId claim if any
            var existingClaims = await _userManager.GetClaimsAsync(user);
            var existingBarangayClaim = existingClaims.FirstOrDefault(c => c.Type == claimType);
            if (existingBarangayClaim != null)
            {
                await _userManager.RemoveClaimAsync(user, existingBarangayClaim);
            }

            // Add new claim if BarangayId is set
            if (barangayId.HasValue)
            {
                await _userManager.AddClaimAsync(user, new Claim(claimType, barangayId.Value.ToString()));
            }
        }

        // GET: /Home/DashboardHome (Legacy - redirects to role-based dashboard)
        [HttpGet]
        public IActionResult DashboardHome()
        {
            if (!IsLoggedIn())
                return RedirectToAction(nameof(Login));

            // Redirect to role-based dashboard
            return RedirectToDashboard();
        }

        // GET: /Home/KnowledgeRepository
        [HttpGet]
        public async Task<IActionResult> KnowledgeRepository(string q = "", string category = "All Categories", string status = "all", string archiveStatus = "active")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            // MULTI-TENANT ISOLATION: Super_admin cannot access barangay internal data
            if (IsSuperAdmin()) return RedirectToAction("System", "Dashboard");

            var role = HttpContext.Session.GetString("Role") ?? "";
            var canUpload = role == "barangay_secretary" || role == "barangay_admin";
            var canApprove = role == "barangay_admin";
            var canArchive = role == "barangay_admin" || role == "super_admin";

            q = (q ?? "").Trim().ToLower();
            category = string.IsNullOrWhiteSpace(category) ? "All Categories" : category.Trim();
            status = string.IsNullOrWhiteSpace(status) ? "all" : status.Trim().ToLower();
            archiveStatus = (archiveStatus ?? "active").Trim().ToLower();

            // ERP Rule: Only admin roles can view archived records
            if (!canArchive) archiveStatus = "active";

            // STRICT TENANT FILTERING: Only users from this barangay can access their data
            var barangayId = GetCurrentBarangayId();
            if (!barangayId.HasValue)
            {
                TempData["Error"] = "No barangay assigned to your account.";
                return RedirectToAction("Barangay", "Dashboard");
            }

            var query = _context.KnowledgeDocuments
                .Where(d => d.IsActive)
                .Where(d => d.BarangayId == barangayId.Value)
                .Include(d => d.UploadedBy)
                .AsQueryable();

            // Filter by archive status
            if (archiveStatus == "active")
                query = query.Where(d => !d.IsArchived);
            else if (archiveStatus == "archived")
                query = query.Where(d => d.IsArchived);
            // "all" shows everything

            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(d =>
                    d.Title.ToLower().Contains(q) ||
                    (d.Tags ?? "").ToLower().Contains(q)
                );
            }

            if (category != "All Categories")
                query = query.Where(d => d.Category == category);

            if (status != "all")
                query = query.Where(d => d.Status.ToLower() == status);

            var docs = await query
                .OrderByDescending(d => d.CreatedAt)
                .Select(d => new RepoDocument
                {
                    Id = d.Id.ToString(),
                    Title = d.Title,
                    Category = d.Category,
                    TagsCsv = d.Tags ?? "",
                    UploadedBy = d.UploadedBy != null ? d.UploadedBy.FullName : "Unknown",
                    UploadedByRole = d.UploadedBy != null ? d.UploadedBy.Role : "",
                    Date = d.CreatedAt.ToString("yyyy-MM-dd"),
                    Status = d.Status,
                    Version = d.Version,
                    Description = d.Description ?? "",
                    FileName = d.FileName ?? "",
                    FilePath = d.FileUrl ?? "",
                    IsArchived = d.IsArchived
                })
                .ToListAsync();

            var vm = new KnowledgeRepositoryViewModel
            {
                SearchQuery = q,
                SelectedCategory = category,
                SelectedStatus = status,
                ArchiveStatus = archiveStatus,
                Categories = new List<string> { "All Categories", "Resolutions", "Ordinances", "Memorandums", "Policies", "Reports" },
                Documents = docs,
                CanUpload = canUpload,
                CanApprove = canApprove,
                CanArchive = canArchive,
                TotalDocuments = await _context.KnowledgeDocuments.CountAsync(d => d.IsActive && !d.IsArchived && d.BarangayId == barangayId),
                ArchivedDocuments = await _context.KnowledgeDocuments.CountAsync(d => d.IsActive && d.IsArchived && d.BarangayId == barangayId),
                PendingDocuments = await _context.KnowledgeDocuments.CountAsync(d => d.IsActive && !d.IsArchived && d.BarangayId == barangayId && d.Status == "pending"),
                SuccessMessage = TempData["Success"] as string,
                ErrorMessage = TempData["Error"] as string,
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
        [RequireActiveSubscription]
        [Authorize(Roles = "super_admin,barangay_admin,barangay_secretary")]
        public async Task<IActionResult> CreateDoc(string title, string category, string tags, string description, IFormFile? file)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            var role = HttpContext.Session.GetString("Role") ?? "";
            var canUpload = role == "barangay_secretary" || role == "barangay_admin" || role == "super_admin";
            if (!canUpload) return RedirectToAction(nameof(KnowledgeRepository));

            title = (title ?? "").Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                TempData["Error"] = "Title is required.";
                return RedirectToAction(nameof(KnowledgeRepository));
            }

            // Get uploading user ID from session
            var userIdStr = HttpContext.Session.GetString("UserId");
            int uploaderId = 0;
            if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out var parsedId))
            {
                uploaderId = parsedId;
            }

            if (uploaderId == 0)
            {
                // Fallback: Try to find user by email (case-insensitive)
                var userEmail = (HttpContext.Session.GetString("UserName") ?? "").ToLower();
                if (!string.IsNullOrEmpty(userEmail))
                {
                    uploaderId = await _context.BusinessUsers
                        .Where(u => u.Email.ToLower() == userEmail && u.IsActive)
                        .Select(u => u.Id)
                        .FirstOrDefaultAsync();

                    // Update session so future requests don't need fallback
                    if (uploaderId > 0)
                    {
                        HttpContext.Session.SetString("UserId", uploaderId.ToString());
                    }
                }
            }

            if (uploaderId == 0)
            {
                TempData["Error"] = "Your account is not linked to a user profile. Please contact the administrator.";
                return RedirectToAction(nameof(KnowledgeRepository));
            }

            // Handle file upload
            string? filePath = null;
            string? fileName = null;
            long? fileSize = null;
            string? fileType = null;

            if (file != null && file.Length > 0)
            {
                // ── File validation (H5 fix) ──
                var allowedDocExtensions = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".jpg", ".jpeg", ".png" };
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedDocExtensions.Contains(ext))
                {
                    TempData["Error"] = "File type not allowed. Accepted: PDF, DOC, DOCX, XLS, XLSX, PPT, PPTX, TXT, JPG, PNG.";
                    return RedirectToAction(nameof(KnowledgeRepository));
                }
                if (file.Length > 25 * 1024 * 1024) // 25 MB limit
                {
                    TempData["Error"] = "File size must be under 25 MB.";
                    return RedirectToAction(nameof(KnowledgeRepository));
                }

                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "documents");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                fileName = Path.GetFileName(file.FileName);
                var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
                var fullPath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                filePath = $"/uploads/documents/{uniqueFileName}";
                fileSize = file.Length;
                fileType = file.ContentType;
            }

            var doc = new KnowledgeDocument
            {
                Title = title,
                Category = string.IsNullOrWhiteSpace(category) ? "Policies" : category.Trim(),
                Tags = (tags ?? "").Trim(),
                Description = (description ?? "").Trim(),
                FileUrl = filePath,
                FileName = fileName,
                FileSize = fileSize,
                FileType = fileType,
                Status = "pending",
                Version = "1.0",
                UploadedById = uploaderId,
                BarangayId = GetCurrentBarangayId(), // AUTO-SET TENANT
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.KnowledgeDocuments.Add(doc);
            await _context.SaveChangesAsync();

            // Send real-time notification to barangay admins
            var barangayId = GetCurrentBarangayId();
            if (barangayId.HasValue)
            {
                var uploaderName = HttpContext.Session.GetString("UserName") ?? "Unknown";
                await _notificationService.NotifyPendingDocument(barangayId.Value, title, uploaderName);
            }

            await LogAuditAsync("Create", "KnowledgeRepository", doc.Id, "Document", title, $"Uploaded document: {title}");

            TempData["Success"] = $"Document uploaded: \"{title}\"";
            return RedirectToAction(nameof(KnowledgeRepository));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
        public async Task<IActionResult> EditDoc(string id, string title, string category, string tags, string description, IFormFile? file)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            var role = HttpContext.Session.GetString("Role") ?? "";
            var canUpload = role == "barangay_secretary" || role == "barangay_admin";
            if (!canUpload) return RedirectToAction(nameof(KnowledgeRepository));

            if (!int.TryParse(id, out var docId))
            {
                TempData["Error"] = "Invalid document ID.";
                return RedirectToAction(nameof(KnowledgeRepository));
            }

            var doc = await _context.KnowledgeDocuments.FindAsync(docId);
            if (doc == null || !doc.IsActive)
            {
                TempData["Error"] = "Document not found.";
                return RedirectToAction(nameof(KnowledgeRepository));
            }

            // STRICT TENANT VALIDATION: Users can only edit their barangay's documents
            if (doc.BarangayId != GetCurrentBarangayId())
            {
                TempData["Error"] = "You cannot edit documents from another barangay.";
                return RedirectToAction(nameof(KnowledgeRepository));
            }

            // Handle file upload (replace existing if new file provided)
            if (file != null && file.Length > 0)
            {
                // ── File validation (H5 fix) ──
                var allowedDocExtensions = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".jpg", ".jpeg", ".png" };
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedDocExtensions.Contains(ext))
                {
                    TempData["Error"] = "File type not allowed. Accepted: PDF, DOC, DOCX, XLS, XLSX, PPT, PPTX, TXT, JPG, PNG.";
                    return RedirectToAction(nameof(KnowledgeRepository));
                }
                if (file.Length > 25 * 1024 * 1024)
                {
                    TempData["Error"] = "File size must be under 25 MB.";
                    return RedirectToAction(nameof(KnowledgeRepository));
                }

                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "documents");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var fileName = Path.GetFileName(file.FileName);
                var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
                var fullPath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                doc.FileUrl = $"/uploads/documents/{uniqueFileName}";
                doc.FileName = fileName;
                doc.FileSize = file.Length;
                doc.FileType = file.ContentType;
            }

            doc.Title = (title ?? doc.Title).Trim();
            doc.Category = string.IsNullOrWhiteSpace(category) ? doc.Category : category.Trim();
            doc.Tags = (tags ?? "").Trim();
            doc.Description = (description ?? "").Trim();
            doc.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            await LogAuditAsync("Edit", "KnowledgeRepository", docId, "Document", doc.Title, $"Updated document: {doc.Title}");

            TempData["Success"] = "Document updated.";
            return RedirectToAction(nameof(KnowledgeRepository));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
        [Authorize(Roles = "super_admin,barangay_admin")]
        public async Task<IActionResult> ArchiveDoc(string id)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            var role = HttpContext.Session.GetString("Role") ?? "";
            var canArchive = role == "barangay_admin" || role == "super_admin";
            if (!canArchive) return RedirectToAction(nameof(KnowledgeRepository));

            if (int.TryParse(id, out var docId))
            {
                var doc = await _context.KnowledgeDocuments.FindAsync(docId);
                if (doc != null)
                {
                    // STRICT TENANT VALIDATION
                    if (doc.BarangayId != GetCurrentBarangayId())
                    {
                        TempData["Error"] = "You cannot archive documents from another barangay.";
                        return RedirectToAction(nameof(KnowledgeRepository));
                    }

                    doc.IsArchived = true;
                    doc.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                    await LogAuditAsync("Archive", "KnowledgeRepository", doc.Id, "Document", doc.Title, $"Archived document: {doc.Title}");
                }
            }

            TempData["Success"] = "Document archived.";
            return RedirectToAction(nameof(KnowledgeRepository));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
        [Authorize(Roles = "super_admin,barangay_admin")]
        public async Task<IActionResult> RestoreDoc(string id)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            var role = HttpContext.Session.GetString("Role") ?? "";
            var canArchive = role == "barangay_admin" || role == "super_admin";
            if (!canArchive) return RedirectToAction(nameof(KnowledgeRepository));

            if (int.TryParse(id, out var docId))
            {
                var doc = await _context.KnowledgeDocuments.FindAsync(docId);
                if (doc != null)
                {
                    // TENANT OWNERSHIP VALIDATION
                    // STRICT TENANT VALIDATION
                    if (doc.BarangayId != GetCurrentBarangayId())
                    {
                        TempData["Error"] = "You cannot restore documents from another barangay.";
                        return RedirectToAction(nameof(KnowledgeRepository));
                    }

                    doc.IsArchived = false;
                    doc.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                    await LogAuditAsync("Restore", "KnowledgeRepository", doc.Id, "Document", doc.Title, $"Restored document: {doc.Title}");
                }
            }

            TempData["Success"] = "Document restored.";
            return RedirectToAction(nameof(KnowledgeRepository), new { archiveStatus = "active" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveDoc(string id)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            var role = HttpContext.Session.GetString("Role") ?? "";
            var canApprove = role == "barangay_admin";
            if (!canApprove) return RedirectToAction(nameof(KnowledgeRepository));

            if (int.TryParse(id, out var docId))
            {
                var doc = await _context.KnowledgeDocuments.FindAsync(docId);
                if (doc != null && doc.IsActive)
                {
                    // STRICT TENANT VALIDATION
                    if (doc.BarangayId != GetCurrentBarangayId())
                    {
                        TempData["Error"] = "You cannot approve documents from another barangay.";
                        return RedirectToAction(nameof(KnowledgeRepository));
                    }

                    doc.Status = "approved";
                    doc.ApprovedAt = DateTime.Now;
                    doc.ApprovedById = GetCurrentUserId();
                    doc.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                    await LogAuditAsync("Approve", "KnowledgeRepository", doc.Id, "Document", doc.Title, $"Approved document: {doc.Title}");

                    // Send real-time notification
                    if (doc.BarangayId.HasValue)
                    {
                        await _notificationService.NotifyDocumentStatusChange(doc.BarangayId.Value, doc.Title, "approved");
                    }
                }
            }

            TempData["Success"] = "Document approved.";
            return RedirectToAction(nameof(KnowledgeRepository));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectDoc(string id)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            var role = HttpContext.Session.GetString("Role") ?? "";
            var canApprove = role == "barangay_admin";
            if (!canApprove) return RedirectToAction(nameof(KnowledgeRepository));

            if (int.TryParse(id, out var docId))
            {
                var doc = await _context.KnowledgeDocuments.FindAsync(docId);
                if (doc != null && doc.IsActive)
                {
                    // STRICT TENANT VALIDATION
                    if (doc.BarangayId != GetCurrentBarangayId())
                    {
                        TempData["Error"] = "You cannot reject documents from another barangay.";
                        return RedirectToAction(nameof(KnowledgeRepository));
                    }

                    doc.Status = "rejected";
                    doc.ApprovedById = GetCurrentUserId(); // Track who rejected
                    doc.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                    await LogAuditAsync("Reject", "KnowledgeRepository", doc.Id, "Document", doc.Title, $"Rejected document: {doc.Title}");

                    // Send real-time notification
                    if (doc.BarangayId.HasValue)
                    {
                        await _notificationService.NotifyDocumentStatusChange(doc.BarangayId.Value, doc.Title, "rejected");
                    }
                }
            }

            TempData["Success"] = "Document rejected.";
            return RedirectToAction(nameof(KnowledgeRepository));
        }

        // GET: /Home/PoliciesProcedures
        [HttpGet]
        public async Task<IActionResult> PoliciesManagement(string status = "all", string q = "", string archiveStatus = "active")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            // MULTI-TENANT ISOLATION: Super_admin cannot access barangay internal data
            if (IsSuperAdmin()) return RedirectToAction("System", "Dashboard");

            var role = HttpContext.Session.GetString("Role") ?? "";
            var canCreate = role == "barangay_secretary" || role == "barangay_admin";
            var canApprove = role == "barangay_admin";
            var canArchive = role == "barangay_admin" || role == "super_admin";

            status = (status ?? "all").Trim().ToLower();
            q = (q ?? "").Trim();
            archiveStatus = (archiveStatus ?? "active").Trim().ToLower();

            // ERP Rule: Only admin roles can view archived records
            if (!canArchive) archiveStatus = "active";

            // STRICT TENANT FILTERING: Get barangay ID and validate
            var barangayIdStr = HttpContext.Session.GetString("BarangayId");
            if (!int.TryParse(barangayIdStr ?? "", out var bgyId) || bgyId == 0)
            {
                TempData["Error"] = "No barangay assigned to your account.";
                return RedirectToAction("Barangay", "Dashboard");
            }

            var query = _context.Policies
                .Where(p => p.IsActive)
                .Where(p => p.BarangayId == bgyId)
                .Include(p => p.Author)
                .AsQueryable();

            // Filter by archive status
            if (archiveStatus == "active")
                query = query.Where(p => !p.IsArchived);
            else if (archiveStatus == "archived")
                query = query.Where(p => p.IsArchived);
            // "all" shows everything

            if (status != "all")
                query = query.Where(p => p.Status.ToLower() == status);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var qq = q.ToLower();
                query = query.Where(p =>
                    p.Title.ToLower().Contains(qq) ||
                    (p.Description ?? "").ToLower().Contains(qq)
                );
            }

            var policies = await query
                .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
                .Select(p => new PolicyItem
                {
                    Id = p.Id.ToString(),
                    Title = p.Title,
                    Description = p.Description ?? "",
                    Status = p.Status,
                    LastUpdated = (p.UpdatedAt ?? p.CreatedAt).ToString("yyyy-MM-dd"),
                    Author = p.Author != null ? p.Author.FullName : "Unknown",
                    Version = p.Version,
                    IsArchived = p.IsArchived
                })
                .ToListAsync();

            // Get counts from active policies in this barangay
            var allPoliciesQuery = _context.Policies.Where(p => p.IsActive);
            if (bgyId > 0)
                allPoliciesQuery = allPoliciesQuery.Where(p => p.BarangayId == bgyId);
            var allPolicies = await allPoliciesQuery.ToListAsync();

            var vm = new PoliciesManagementViewModel
            {
                StatusFilter = status,
                SearchQuery = q,
                ArchiveStatus = archiveStatus,
                CanCreate = canCreate,
                CanApprove = canApprove,
                CanArchive = canArchive,

                CountAll = allPolicies.Count(x => !x.IsArchived),
                CountApproved = allPolicies.Count(x => !x.IsArchived && x.Status == "approved"),
                CountPending = allPolicies.Count(x => !x.IsArchived && x.Status == "pending"),
                CountDraft = allPolicies.Count(x => !x.IsArchived && x.Status == "draft"),
                CountArchived = allPolicies.Count(x => x.IsArchived),

                Policies = policies
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
        [RequireActiveSubscription]
        [Authorize(Roles = "super_admin,barangay_admin,barangay_secretary")]
        public async Task<IActionResult> CreatePolicy(string title, string description, string status = "all", string q = "")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            var role = HttpContext.Session.GetString("Role") ?? "";
            var canCreate = role == "barangay_secretary" || role == "barangay_admin" || role == "super_admin";
            if (!canCreate) return RedirectToAction(nameof(PoliciesManagement), new { status, q });

            title = (title ?? "").Trim();
            description = (description ?? "").Trim();

            if (string.IsNullOrWhiteSpace(title))
            {
                TempData["Error"] = "Title is required.";
                return RedirectToAction(nameof(PoliciesManagement), new { status, q });
            }

            // Get author ID from session email
            var userEmail = HttpContext.Session.GetString("UserName") ?? "";
            var authorId = await _context.BusinessUsers
                .Where(u => u.Email == userEmail)
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            if (authorId == 0) authorId = 1;

            // Get BarangayId from session
            var barangayIdStr = HttpContext.Session.GetString("BarangayId");
            int.TryParse(barangayIdStr, out var bgyId);

            // If admin creates it, auto-approve; otherwise set to pending for admin approval
            var initialStatus = (role == "barangay_admin") ? "approved" : "pending";

            var policy = new Policy
            {
                Title = title,
                Description = description,
                Status = initialStatus,
                Version = "1.0",
                AuthorId = authorId,
                BarangayId = bgyId > 0 ? bgyId : null,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            // If admin auto-approved, set approval fields
            if (initialStatus == "approved")
            {
                policy.ApprovedById = authorId;
                policy.ApprovedAt = DateTime.Now;
            }

            _context.Policies.Add(policy);
            await _context.SaveChangesAsync();

            // Send real-time notification to admins if pending
            if (initialStatus == "pending" && bgyId > 0)
            {
                await _notificationService.NotifyPendingPolicy(bgyId, title, userEmail);
            }

            await LogAuditAsync("Create", "PoliciesManagement", policy.Id, "Policy", title, $"Created policy: {title}");

            TempData["Success"] = $"Policy \"{title}\" created successfully.";
            return RedirectToAction(nameof(PoliciesManagement), new { status, q });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
        public async Task<IActionResult> EditPolicy(string id, string title, string description, string status = "all", string q = "")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            var role = HttpContext.Session.GetString("Role") ?? "";
            var canEdit = role == "barangay_secretary" || role == "barangay_admin" || role == "super_admin";
            if (!canEdit) return RedirectToAction(nameof(PoliciesManagement), new { status, q });

            if (!int.TryParse(id, out var policyId))
            {
                TempData["Error"] = "Invalid policy ID.";
                return RedirectToAction(nameof(PoliciesManagement), new { status, q });
            }

            var policy = await _context.Policies.FindAsync(policyId);
            if (policy == null || !policy.IsActive)
            {
                TempData["Error"] = "Policy not found.";
                return RedirectToAction(nameof(PoliciesManagement), new { status, q });
            }

            // STRICT TENANT VALIDATION
            if (policy.BarangayId != GetCurrentBarangayId())
            {
                TempData["Error"] = "You cannot edit policies from another barangay.";
                return RedirectToAction(nameof(PoliciesManagement), new { status, q });
            }

            policy.Title = (title ?? policy.Title).Trim();
            policy.Description = (description ?? "").Trim();
            policy.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            await LogAuditAsync("Edit", "PoliciesManagement", policyId, "Policy", policy.Title, $"Updated policy: {policy.Title}");

            TempData["Success"] = "Policy updated successfully.";
            return RedirectToAction(nameof(PoliciesManagement), new { status, q });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
        [Authorize(Roles = "super_admin,barangay_admin")]
        public async Task<IActionResult> ArchivePolicy(string id, string status = "all", string q = "")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            var role = HttpContext.Session.GetString("Role") ?? "";
            var canArchive = role == "barangay_admin" || role == "super_admin";
            if (!canArchive) return RedirectToAction(nameof(PoliciesManagement), new { status, q });

            if (int.TryParse(id, out var policyId))
            {
                var policy = await _context.Policies.FindAsync(policyId);
                if (policy != null)
                {
                    // STRICT TENANT VALIDATION
                    if (policy.BarangayId != GetCurrentBarangayId())
                    {
                        TempData["Error"] = "You cannot archive policies from another barangay.";
                        return RedirectToAction(nameof(PoliciesManagement), new { status, q });
                    }

                    policy.IsArchived = true;
                    policy.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                    await LogAuditAsync("Archive", "PoliciesManagement", policy.Id, "Policy", policy.Title, $"Archived policy: {policy.Title}");
                }
            }

            TempData["Success"] = "Policy archived.";
            return RedirectToAction(nameof(PoliciesManagement), new { status, q });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
        [Authorize(Roles = "super_admin,barangay_admin")]
        public async Task<IActionResult> RestorePolicy(string id, string status = "all", string q = "")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            var role = HttpContext.Session.GetString("Role") ?? "";
            var canArchive = role == "barangay_admin" || role == "super_admin";
            if (!canArchive) return RedirectToAction(nameof(PoliciesManagement), new { status, q });

            if (int.TryParse(id, out var rpolicyId))
            {
                var policy = await _context.Policies.FindAsync(rpolicyId);
                if (policy != null)
                {
                    // STRICT TENANT VALIDATION
                    if (policy.BarangayId != GetCurrentBarangayId())
                    {
                        TempData["Error"] = "You cannot restore policies from another barangay.";
                        return RedirectToAction(nameof(PoliciesManagement), new { status, q });
                    }

                    policy.IsArchived = false;
                    policy.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                    await LogAuditAsync("Restore", "PoliciesManagement", policy.Id, "Policy", policy.Title, $"Restored policy: {policy.Title}");
                }
            }

            TempData["Success"] = "Policy restored.";
            return RedirectToAction(nameof(PoliciesManagement), new { status, q, archiveStatus = "active" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPolicyStatus(string id, string newStatus, string status = "all", string q = "")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            var role = HttpContext.Session.GetString("Role") ?? "";
            var canApprove = role == "barangay_admin";
            if (!canApprove) return RedirectToAction(nameof(PoliciesManagement), new { status, q });

            newStatus = (newStatus ?? "").Trim().ToLower();
            if (newStatus != "approved" && newStatus != "rejected" && newStatus != "pending" && newStatus != "draft")
                newStatus = "draft";

            if (int.TryParse(id, out var policyId))
            {
                var policy = await _context.Policies.FindAsync(policyId);
                if (policy != null && policy.IsActive)
                {
                    // STRICT TENANT VALIDATION
                    if (policy.BarangayId != GetCurrentBarangayId())
                    {
                        TempData["Error"] = "You cannot change the status of policies from another barangay.";
                        return RedirectToAction(nameof(PoliciesManagement), new { status, q });
                    }

                    policy.Status = newStatus;
                    policy.UpdatedAt = DateTime.Now;

                    if (newStatus == "approved")
                    {
                        var userEmail = HttpContext.Session.GetString("UserName") ?? "";
                        var approverId = await _context.BusinessUsers
                            .Where(u => u.Email == userEmail)
                            .Select(u => u.Id)
                            .FirstOrDefaultAsync();
                        if (approverId > 0)
                        {
                            policy.ApprovedById = approverId;
                            policy.ApprovedAt = DateTime.Now;
                        }
                    }

                    await _context.SaveChangesAsync();
                    await LogAuditAsync("StatusChange", "PoliciesManagement", policy.Id, "Policy", policy.Title, $"Changed policy status to {newStatus}");

                    // Send real-time notification
                    if (policy.BarangayId.HasValue && (newStatus == "approved" || newStatus == "rejected"))
                    {
                        await _notificationService.NotifyPolicyStatusChange(policy.BarangayId.Value, policy.Title, newStatus);
                    }
                }
            }

            TempData["Success"] = $"Policy status set to {newStatus}.";
            return RedirectToAction(nameof(PoliciesManagement), new { status, q });
        }

        // GET: /Home/LessonsLearned
        public async Task<IActionResult> LessonsLearned(string q = "", string dateFilter = "", string archiveStatus = "active")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            // MULTI-TENANT ISOLATION: Super_admin cannot access barangay internal data
            if (IsSuperAdmin()) return RedirectToAction("System", "Dashboard");

            var role = HttpContext.Session.GetString("Role") ?? "";
            var barangayId = GetCurrentBarangayId();
            
            if (!barangayId.HasValue)
            {
                TempData["Error"] = "No barangay assigned to your account.";
                return RedirectToAction("Barangay", "Dashboard");
            }

            bool canSubmit = role == "barangay_staff" || role == "barangay_secretary" || role == "barangay_admin";
            bool canModify = role == "barangay_admin" || role == "barangay_secretary";
            bool canArchive = role == "barangay_admin" || role == "super_admin";
            bool canApprove = role == "barangay_admin" || role == "super_admin";

            q = (q ?? "").Trim().ToLower();
            dateFilter = (dateFilter ?? "").Trim();
            archiveStatus = (archiveStatus ?? "active").Trim().ToLower();

            // ERP Rule: Only admin roles can view archived records
            if (!canArchive) archiveStatus = "active";

            // STRICT TENANT FILTERING: Only this barangay's data
            var query = _context.LessonsLearned
                .Where(l => l.IsActive)
                .Where(l => l.BarangayId == barangayId.Value);

            // Filter by archive status
            if (archiveStatus == "active")
                query = query.Where(l => !l.IsArchived);
            else if (archiveStatus == "archived")
                query = query.Where(l => l.IsArchived);
            // "all" shows everything

            // Search by Title/Problem
            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(l => l.Title.ToLower().Contains(q) || l.Problem.ToLower().Contains(q));
            }

            // Filter by date (month-year)
            if (!string.IsNullOrWhiteSpace(dateFilter) && dateFilter != "All Dates")
            {
                var parts = dateFilter.Split('-');
                if (parts.Length == 2 && int.TryParse(parts[0], out int year) && int.TryParse(parts[1], out int month))
                {
                    query = query.Where(l => l.DateRecorded.Year == year && l.DateRecorded.Month == month);
                }
            }

            var lessons = await query
                .OrderByDescending(l => l.DateRecorded)
                .Select(l => new LessonRow
                {
                    Id = l.Id,
                    Title = l.Title,
                    Problem = l.Problem,
                    ActionTaken = l.ActionTaken,
                    Result = l.Result,
                    Recommendation = l.Recommendation ?? "",
                    DateRecorded = l.DateRecorded,
                    Summary = l.Summary,
                    Project = l.ProjectName ?? "",
                    Status = l.Status,
                    Date = l.DateRecorded.ToString("MMM dd, yyyy"),
                    IsArchived = l.IsArchived,
                    Likes = l.LikesCount,
                    Comments = l.CommentsCount,
                    Tags = new List<string>()
                })
                .ToListAsync();

            // Parse tags after query
            foreach (var lesson in lessons)
            {
                if (!string.IsNullOrWhiteSpace(lesson.Project))
                {
                    // Tags were stored as comma-separated, but we're not using them now
                }
            }

            // Available dates for filter (strict tenant filtering)
            var datesQuery = _context.LessonsLearned
                .Where(l => l.IsActive && !l.IsArchived)
                .Where(l => l.BarangayId == barangayId.Value);
            
            var availableDates = await datesQuery
                .Select(l => l.DateRecorded)
                .Distinct()
                .OrderByDescending(d => d)
                .Select(d => $"{d.Year}-{d.Month:D2}")
                .Distinct()
                .Take(12)
                .ToListAsync();
            availableDates.Insert(0, "All Dates");

            var projectTypes = new List<string>
            {
                "All Projects", "Health Program", "Finance Modernization", "Youth Development",
                "Disaster Risk Reduction", "Education", "Environment"
            };

            // Count queries with strict tenant filtering
            var totalQuery = _context.LessonsLearned
                .Where(l => l.IsActive && !l.IsArchived)
                .Where(l => l.BarangayId == barangayId.Value);
            var archivedQuery = _context.LessonsLearned
                .Where(l => l.IsActive && l.IsArchived)
                .Where(l => l.BarangayId == barangayId.Value);
            var recentQuery = _context.LessonsLearned
                .Where(l => l.IsActive && !l.IsArchived && l.DateRecorded >= DateTime.Now.AddDays(-30))
                .Where(l => l.BarangayId == barangayId.Value);

            var vm = new LessonsLearnedViewModel
            {
                CanSubmit = canSubmit,
                CanModify = canModify,
                CanArchive = canArchive,
                CanApprove = canApprove,
                TotalLessons = await totalQuery.CountAsync(),
                RecentLessons = await recentQuery.CountAsync(),
                ArchivedLessons = await archivedQuery.CountAsync(),
                SearchQuery = q,
                DateFilter = dateFilter,
                ArchiveStatus = archiveStatus,
                AvailableDates = availableDates,
                Lessons = lessons,
                ProjectTypes = projectTypes
            };

            if (TempData["Success"] != null)
                vm.SuccessMessage = TempData["Success"]?.ToString();
            if (TempData["Error"] != null)
                vm.ErrorMessage = TempData["Error"]?.ToString();

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
        [RequireActiveSubscription]
        [Authorize(Roles = "super_admin,barangay_admin,barangay_secretary,barangay_staff")]
        public async Task<IActionResult> CreateLesson(string title, string problem, string actionTaken, string result, string recommendation)
        {
      if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
        var role = HttpContext.Session.GetString("Role") ?? "";
        if (role != "barangay_admin" && role != "barangay_secretary" && role != "barangay_staff" && role != "super_admin") {
            TempData["Error"] = "You do not have permission to submit lessons.";
            return RedirectToAction(nameof(LessonsLearned));
        }

            var barangayId = GetCurrentBarangayId();
            var userId = GetCurrentUserId() ?? 0;

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(problem) ||
                string.IsNullOrWhiteSpace(actionTaken) || string.IsNullOrWhiteSpace(result))
            {
                TempData["Error"] = "Title, Problem, Action Taken, and Result are required.";
                return RedirectToAction(nameof(LessonsLearned));
            }

            // If admin creates it, auto-approve; otherwise set to pending for admin approval
            var initialStatus = (role == "barangay_admin" || role == "super_admin") ? "approved" : "pending";

            var lesson = new LessonLearned
            {
                Title = title.Trim(),
                Problem = problem.Trim(),
                ActionTaken = actionTaken.Trim(),
                Result = result.Trim(),
                Recommendation = recommendation?.Trim(),
                Summary = problem.Trim(),
                DateRecorded = DateTime.Now,
                BarangayId = barangayId,
                SubmittedById = userId,
                Status = initialStatus,
                CreatedAt = DateTime.Now,
                IsArchived = false
            };

            // If admin auto-approved, set approval fields
            if (initialStatus == "approved")
            {
                lesson.ApprovedById = userId;
                lesson.ApprovedAt = DateTime.Now;
            }

            _context.LessonsLearned.Add(lesson);
            await _context.SaveChangesAsync();
            await LogAuditAsync("Create", "LessonsLearned", lesson.Id, "Lesson", title, $"Created lesson: {title}");

            // Send real-time notification to admins if pending
            if (initialStatus == "pending" && barangayId.HasValue)
            {
                var submitterName = HttpContext.Session.GetString("UserName") ?? "Unknown";
                await _notificationService.NotifyPendingLesson(barangayId.Value, title, submitterName);
            }

            TempData["Success"] = "Lesson learned has been created.";
            return RedirectToAction(nameof(LessonsLearned));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
        public async Task<IActionResult> EditLesson(int id, string title, string problem, string actionTaken, string result, string recommendation)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            var role = HttpContext.Session.GetString("Role") ?? "";
            if (role != "barangay_admin" && role != "barangay_secretary")
                return RedirectToAction(nameof(LessonsLearned));

            var lesson = await _context.LessonsLearned.FindAsync(id);
            if (lesson == null || lesson.IsArchived)
            {
                TempData["Error"] = "Lesson not found.";
                return RedirectToAction(nameof(LessonsLearned));
            }

            // TENANT OWNERSHIP VALIDATION
            // STRICT TENANT VALIDATION
            if (lesson.BarangayId != GetCurrentBarangayId())
            {
                TempData["Error"] = "You cannot edit lessons from another barangay.";
                return RedirectToAction(nameof(LessonsLearned));
            }

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(problem) ||
                string.IsNullOrWhiteSpace(actionTaken) || string.IsNullOrWhiteSpace(result))
            {
                TempData["Error"] = "Title, Problem, Action Taken, and Result are required.";
                return RedirectToAction(nameof(LessonsLearned));
            }

            lesson.Title = title.Trim();
            lesson.Problem = problem.Trim();
            lesson.ActionTaken = actionTaken.Trim();
            lesson.Result = result.Trim();
            lesson.Recommendation = recommendation?.Trim();
            lesson.Summary = problem.Trim();
            lesson.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            await LogAuditAsync("Edit", "LessonsLearned", lesson.Id, "Lesson", lesson.Title, $"Updated lesson: {lesson.Title}");

            TempData["Success"] = "Lesson has been updated.";
            return RedirectToAction(nameof(LessonsLearned));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
        [Authorize(Roles = "super_admin,barangay_admin")]
        public async Task<IActionResult> ArchiveLesson(int id)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            var role = HttpContext.Session.GetString("Role") ?? "";
            if (role != "barangay_admin" && role != "super_admin")
                return RedirectToAction(nameof(LessonsLearned));

            var lesson = await _context.LessonsLearned.FindAsync(id);
            if (lesson != null)
            {
                // STRICT TENANT VALIDATION
                if (lesson.BarangayId != GetCurrentBarangayId())
                {
                    TempData["Error"] = "You cannot archive lessons from another barangay.";
                    return RedirectToAction(nameof(LessonsLearned));
                }

                lesson.IsArchived = true;
                lesson.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                await LogAuditAsync("Archive", "LessonsLearned", lesson.Id, "Lesson", lesson.Title, $"Archived lesson: {lesson.Title}");
                TempData["Success"] = "Lesson has been archived.";
            }

            return RedirectToAction(nameof(LessonsLearned));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
        [Authorize(Roles = "super_admin,barangay_admin")]
        public async Task<IActionResult> RestoreLesson(int id)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            var role = HttpContext.Session.GetString("Role") ?? "";
            if (role != "barangay_admin" && role != "super_admin")
                return RedirectToAction(nameof(LessonsLearned));

            var lesson = await _context.LessonsLearned.FindAsync(id);
            if (lesson != null)
            {
                // STRICT TENANT VALIDATION
                if (lesson.BarangayId != GetCurrentBarangayId())
                {
                    TempData["Error"] = "You cannot restore lessons from another barangay.";
                    return RedirectToAction(nameof(LessonsLearned));
                }

                lesson.IsArchived = false;
                lesson.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                await LogAuditAsync("Restore", "LessonsLearned", lesson.Id, "Lesson", lesson.Title, $"Restored lesson: {lesson.Title}");
                TempData["Success"] = "Lesson has been restored.";
            }

            return RedirectToAction(nameof(LessonsLearned), new { archiveStatus = "active" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
        [Authorize(Roles = "super_admin,barangay_admin")]
        public async Task<IActionResult> SetLessonStatus(int id, string newStatus)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            var role = HttpContext.Session.GetString("Role") ?? "";
            var canApprove = role == "barangay_admin" || role == "super_admin";
            if (!canApprove) return RedirectToAction(nameof(LessonsLearned));

            newStatus = (newStatus ?? "").Trim().ToLower();
            if (newStatus != "approved" && newStatus != "rejected" && newStatus != "pending")
                newStatus = "pending";

            var lesson = await _context.LessonsLearned.FindAsync(id);
            if (lesson != null && !lesson.IsArchived)
            {
                // STRICT TENANT VALIDATION
                if (lesson.BarangayId != GetCurrentBarangayId())
                {
                    TempData["Error"] = "You cannot change the status of lessons from another barangay.";
                    return RedirectToAction(nameof(LessonsLearned));
                }

                lesson.Status = newStatus;
                lesson.UpdatedAt = DateTime.Now;

                if (newStatus == "approved")
                {
                    var userId = GetCurrentUserId() ?? 0;
                    if (userId > 0)
                    {
                        lesson.ApprovedById = userId;
                        lesson.ApprovedAt = DateTime.Now;
                    }
                }

                await _context.SaveChangesAsync();
                await LogAuditAsync(newStatus == "approved" ? "Approve" : "Reject", "LessonsLearned", lesson.Id, "Lesson", lesson.Title, $"Changed lesson status to {newStatus}: {lesson.Title}");

                // Send real-time notification
                if (lesson.BarangayId.HasValue)
                {
                    await _notificationService.NotifyLessonStatusChange(lesson.BarangayId.Value, lesson.Title, newStatus);
                }
            }

            TempData["Success"] = $"Lesson status set to {newStatus}.";
            return RedirectToAction(nameof(LessonsLearned));
        }

        // GET: /Home/BestPractices
        [HttpGet]
        public async Task<IActionResult> BestPractices(string q = "", string status = "", string archiveStatus = "active")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            // MULTI-TENANT ISOLATION: Super_admin cannot access barangay internal data
            if (IsSuperAdmin()) return RedirectToAction("System", "Dashboard");

            q = (q ?? "").Trim().ToLower();
            status = (status ?? "").Trim();
            archiveStatus = (archiveStatus ?? "active").Trim().ToLower();

            var role = HttpContext.Session.GetString("Role") ?? "";
            var barangayId = GetCurrentBarangayId();
            
            if (!barangayId.HasValue)
            {
                TempData["Error"] = "No barangay assigned to your account.";
                return RedirectToAction("Barangay", "Dashboard");
            }

            bool canManage = role == "barangay_admin";
            bool canModify = canManage;
            bool canArchive = role == "barangay_admin" || role == "super_admin";
            bool canApprove = role == "barangay_admin" || role == "super_admin";

            // ERP Rule: Only admin roles can view archived records
            if (!canArchive) archiveStatus = "active";

            var query = _context.BestPractices
                .Where(p => p.IsActive);

            // Filter by barangay (only if user has barangayId - super_admin sees all)
            if (barangayId.HasValue)
                query = query.Where(p => p.BarangayId == barangayId);

            // Filter by archive status
            if (archiveStatus == "active")
                query = query.Where(p => !p.IsArchived);
            else if (archiveStatus == "archived")
                query = query.Where(p => p.IsArchived);
            // "all" shows everything

            // Search by Title
            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(p => p.Title.ToLower().Contains(q));
            }

            // Filter by Status
            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                query = query.Where(p => p.Status == status);
            }

            var practices = await query
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new BestPracticeItem
                {
                    Id = p.Id,
                    Title = p.Title,
                    Description = p.Description,
                    Purpose = p.Purpose ?? "",
                    Steps = p.Steps,
                    ResourcesNeeded = p.ResourcesNeeded ?? "",
                    OwnerOffice = p.OwnerOffice ?? "",
                    Category = p.Category,
                    Status = p.Status,
                    Rating = p.Rating,
                    Implementations = p.Implementations,
                    IsFeatured = p.IsFeatured,
                    IsArchived = p.IsArchived,
                    CreatedAt = p.CreatedAt,
                    Barangay = !string.IsNullOrEmpty(p.BarangayName)
                        ? "Purok " + p.BarangayName.Replace("Brgy. ", "").Replace("Brgy ", "")
                        : "",
                    DateAdded = p.CreatedAt.ToString("yyyy-MM-dd")
                })
                .ToListAsync();

            var vm = new BestPracticesViewModel
            {
                SearchQuery = q,
                SelectedStatus = status,
                ArchiveStatus = archiveStatus,
                CanManage = canManage,
                CanModify = canModify,
                CanArchive = canArchive,
                CanApprove = canApprove,
                TotalPractices = await _context.BestPractices.CountAsync(p => p.IsActive && !p.IsArchived && p.BarangayId == barangayId),
                ActivePractices = await _context.BestPractices.CountAsync(p => p.IsActive && !p.IsArchived && p.BarangayId == barangayId && p.Status == "Active"),
                ArchivedPractices = await _context.BestPractices.CountAsync(p => p.IsActive && p.IsArchived && p.BarangayId == barangayId),
                Categories = new List<string> { "All Categories", "Health", "Education", "Governance", "Environment", "Safety", "Finance" },
                Practices = practices
            };

            if (TempData["Success"] != null)
                vm.SuccessMessage = TempData["Success"]?.ToString();
            if (TempData["Error"] != null)
                vm.ErrorMessage = TempData["Error"]?.ToString();

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
        [RequireActiveSubscription]
        [Authorize(Roles = "super_admin,barangay_admin")]
        public async Task<IActionResult> CreatePractice(string title, string purpose, string steps, string resourcesNeeded, string ownerOffice, string category)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            var role = HttpContext.Session.GetString("Role") ?? "";
            if (role != "barangay_admin" && role != "super_admin")
                return RedirectToAction(nameof(BestPractices));

            var barangayId = GetCurrentBarangayId();
            var userId = GetCurrentUserId() ?? 0;

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(steps))
            {
                TempData["Error"] = "Title and Steps are required.";
                return RedirectToAction(nameof(BestPractices));
            }

            // If admin creates it, auto-approve; otherwise set to pending for admin approval
            var initialStatus = (role == "barangay_admin" || role == "super_admin") ? "approved" : "pending";

            var practice = new BestPractice
            {
                Title = title.Trim(),
                Description = purpose?.Trim() ?? "",
                Purpose = purpose?.Trim(),
                Steps = steps.Trim(),
                ResourcesNeeded = resourcesNeeded?.Trim(),
                OwnerOffice = ownerOffice?.Trim(),
                Category = string.IsNullOrWhiteSpace(category) ? "Governance" : category.Trim(),
                Status = initialStatus,
                BarangayId = barangayId,
                SubmittedById = userId,
                CreatedAt = DateTime.Now,
                IsArchived = false
            };

            // If admin auto-approved, set approval fields
            if (initialStatus == "approved")
            {
                practice.ApprovedById = userId;
                practice.ApprovedAt = DateTime.Now;
            }

            _context.BestPractices.Add(practice);
            await _context.SaveChangesAsync();
            await LogAuditAsync("Create", "BestPractices", practice.Id, "BestPractice", title, $"Created best practice: {title}");

            // Send real-time notification to admins if pending
            if (initialStatus == "pending" && barangayId.HasValue)
            {
                var submitterName = HttpContext.Session.GetString("UserName") ?? "Unknown";
                await _notificationService.NotifyPendingPractice(barangayId.Value, title, submitterName);
            }

            TempData["Success"] = "Best practice has been created.";
            return RedirectToAction(nameof(BestPractices));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
        public async Task<IActionResult> EditPractice(int id, string title, string purpose, string steps, string resourcesNeeded, string ownerOffice, string category, string status)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            var role = HttpContext.Session.GetString("Role") ?? "";
            if (role != "barangay_admin")
                return RedirectToAction(nameof(BestPractices));

            var practice = await _context.BestPractices.FindAsync(id);
            if (practice == null || practice.IsArchived)
            {
                TempData["Error"] = "Practice not found.";
                return RedirectToAction(nameof(BestPractices));
            }

            // TENANT OWNERSHIP VALIDATION
            // STRICT TENANT VALIDATION
            if (practice.BarangayId != GetCurrentBarangayId())
            {
                TempData["Error"] = "You cannot edit practices from another barangay.";
                return RedirectToAction(nameof(BestPractices));
            }

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(steps))
            {
                TempData["Error"] = "Title and Steps are required.";
                return RedirectToAction(nameof(BestPractices));
            }

            practice.Title = title.Trim();
            practice.Description = purpose?.Trim() ?? "";
            practice.Purpose = purpose?.Trim();
            practice.Steps = steps.Trim();
            practice.ResourcesNeeded = resourcesNeeded?.Trim();
            practice.OwnerOffice = ownerOffice?.Trim();
            practice.Category = string.IsNullOrWhiteSpace(category) ? practice.Category : category.Trim();
            practice.Status = string.IsNullOrWhiteSpace(status) ? practice.Status : status.Trim();
            practice.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            await LogAuditAsync("Edit", "BestPractices", practice.Id, "BestPractice", practice.Title, $"Updated best practice: {practice.Title}");

            TempData["Success"] = "Best practice has been updated.";
            return RedirectToAction(nameof(BestPractices));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
        [Authorize(Roles = "super_admin,barangay_admin")]
        public async Task<IActionResult> ArchivePractice(int id)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            var role = HttpContext.Session.GetString("Role") ?? "";
            if (role != "barangay_admin" && role != "super_admin")
                return RedirectToAction(nameof(BestPractices));

            var practice = await _context.BestPractices.FindAsync(id);
            if (practice != null)
            {
                // STRICT TENANT VALIDATION
                if (practice.BarangayId != GetCurrentBarangayId())
                {
                    TempData["Error"] = "You cannot archive practices from another barangay.";
                    return RedirectToAction(nameof(BestPractices));
                }

                practice.IsArchived = true;
                practice.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                await LogAuditAsync("Archive", "BestPractices", practice.Id, "BestPractice", practice.Title, $"Archived best practice: {practice.Title}");
                TempData["Success"] = "Practice has been archived.";
            }

            return RedirectToAction(nameof(BestPractices));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
        [Authorize(Roles = "super_admin,barangay_admin")]
        public async Task<IActionResult> RestorePractice(int id)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            var role = HttpContext.Session.GetString("Role") ?? "";
            if (role != "barangay_admin" && role != "super_admin")
                return RedirectToAction(nameof(BestPractices));

            var practice = await _context.BestPractices.FindAsync(id);
            if (practice != null)
            {
                // STRICT TENANT VALIDATION
                if (practice.BarangayId != GetCurrentBarangayId())
                {
                    TempData["Error"] = "You cannot restore practices from another barangay.";
                    return RedirectToAction(nameof(BestPractices));
                }

                practice.IsArchived = false;
                practice.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                await LogAuditAsync("Restore", "BestPractices", practice.Id, "BestPractice", practice.Title, $"Restored best practice: {practice.Title}");
                TempData["Success"] = "Practice has been restored.";
            }

            return RedirectToAction(nameof(BestPractices), new { archiveStatus = "active" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
        [Authorize(Roles = "super_admin,barangay_admin")]
        public async Task<IActionResult> SetBestPracticeStatus(int id, string newStatus)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            var role = HttpContext.Session.GetString("Role") ?? "";
            var canApprove = role == "barangay_admin" || role == "super_admin";
            if (!canApprove) return RedirectToAction(nameof(BestPractices));

            newStatus = (newStatus ?? "").Trim().ToLower();
            if (newStatus != "approved" && newStatus != "rejected" && newStatus != "pending")
                newStatus = "pending";

            var practice = await _context.BestPractices.FindAsync(id);
            if (practice != null && !practice.IsArchived)
            {
                // STRICT TENANT VALIDATION
                if (practice.BarangayId != GetCurrentBarangayId())
                {
                    TempData["Error"] = "You cannot change the status of practices from another barangay.";
                    return RedirectToAction(nameof(BestPractices));
                }

                practice.Status = newStatus;
                practice.UpdatedAt = DateTime.Now;

                if (newStatus == "approved")
                {
                    var userId = GetCurrentUserId() ?? 0;
                    if (userId > 0)
                    {
                        practice.ApprovedById = userId;
                        practice.ApprovedAt = DateTime.Now;
                    }
                }

                await _context.SaveChangesAsync();
                await LogAuditAsync(newStatus == "approved" ? "Approve" : "Reject", "BestPractices", practice.Id, "BestPractice", practice.Title, $"Changed practice status to {newStatus}: {practice.Title}");

                // Send real-time notification
                if (practice.BarangayId.HasValue)
                {
                    await _notificationService.NotifyPracticeStatusChange(practice.BarangayId.Value, practice.Title, newStatus);
                }
            }

            TempData["Success"] = $"Practice status set to {newStatus}.";
            return RedirectToAction(nameof(BestPractices));
        }

        // GET: /Home/KnowledgeSharing
        [HttpGet]
        public async Task<IActionResult> KnowledgeSharing(string q = "", string category = "All Categories", string archiveStatus = "active")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            // MULTI-TENANT ISOLATION: Super_admin cannot access barangay internal data
            if (IsSuperAdmin()) return RedirectToAction("System", "Dashboard");

            var role = HttpContext.Session.GetString("Role") ?? "";
            var barangayId = GetCurrentBarangayId();
            
            if (!barangayId.HasValue)
            {
                TempData["Error"] = "No barangay assigned to your account.";
                return RedirectToAction("Barangay", "Dashboard");
            }

            bool canPost = !string.IsNullOrEmpty(role); // All logged-in users can post
            bool canAnnounce = role == "barangay_admin" || role == "super_admin";
            bool canArchive = role == "barangay_admin" || role == "super_admin";

            q = (q ?? "").Trim().ToLower();
            category = string.IsNullOrWhiteSpace(category) ? "All Categories" : category.Trim();
            archiveStatus = (archiveStatus ?? "active").Trim().ToLower();

            // ERP Rule: Only admin roles can view archived records
            if (!canArchive) archiveStatus = "active";

            // STRICT TENANT FILTERING: Query discussions from user's barangay only
            var query = _context.KnowledgeDiscussions
                .Where(d => d.IsActive)
                .Where(d => d.BarangayId == barangayId.Value)
                .Include(d => d.Author)
                .AsQueryable();

            // Filter by archive status
            if (archiveStatus == "active")
                query = query.Where(d => !d.IsArchived);
            else if (archiveStatus == "archived")
                query = query.Where(d => d.IsArchived);
            // "all" shows everything

            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(d =>
                    d.Title.ToLower().Contains(q) ||
                    d.Content.ToLower().Contains(q)
                );
            }

            if (category != "All Categories")
                query = query.Where(d => d.Category == category);

            // Get current user ID for tracking likes
            var userIdStr = HttpContext.Session.GetString("UserId");
            int.TryParse(userIdStr, out var currentUserId);

            // Get discussion IDs first
            var discussionIds = await query.Select(d => d.Id).ToListAsync();

            // Get likes by current user
            var userLikes = await _context.DiscussionLikes
                .Where(l => discussionIds.Contains(l.DiscussionId) && l.UserId == currentUserId)
                .Select(l => l.DiscussionId)
                .ToListAsync();

            // Get comments for all discussions
            var allComments = await _context.DiscussionComments
                .Where(c => discussionIds.Contains(c.DiscussionId) && c.IsActive)
                .Include(c => c.Author)
                .OrderBy(c => c.CreatedAt)
                .Select(c => new { 
                    c.DiscussionId, 
                    c.Id, 
                    AuthorName = c.Author != null ? c.Author.FullName : "Unknown",
                    c.Content, 
                    c.CreatedAt 
                })
                .ToListAsync();

            var discussions = await query
                .OrderByDescending(d => d.CreatedAt)
                .Select(d => new KnowledgeDiscussionItem
                {
                    Id = d.Id.ToString(),
                    Title = d.Title,
                    Content = d.Content,
                    Author = d.Author != null ? d.Author.FullName : "Unknown",
                    Avatar = "",
                    Date = d.CreatedAt.ToString("MMM dd, yyyy"),
                    Category = d.Category ?? "",
                    Replies = d.RepliesCount,
                    Likes = d.LikesCount,
                    IsArchived = d.IsArchived
                })
                .ToListAsync();

            // Populate UserHasLiked and Comments
            foreach (var d in discussions)
            {
                if (int.TryParse(d.Id, out var dId))
                {
                    d.UserHasLiked = userLikes.Contains(dId);
                    d.Comments = allComments
                        .Where(c => c.DiscussionId == dId)
                        .Select(c => new DiscussionCommentItem
                        {
                            Id = c.Id,
                            AuthorName = c.AuthorName,
                            AuthorInitials = GetInitials(c.AuthorName),
                            Content = c.Content,
                            Date = c.CreatedAt.ToString("MMM dd, yyyy HH:mm")
                        })
                        .ToList();
                }
            }

            // Fetch active members from barangay (users who logged in within last 30 days)
            var thirtyDaysAgo = DateTime.Now.AddDays(-30);
            var activeMembersList = await _context.BusinessUsers
                .Where(u => u.IsActive && u.BarangayId == barangayId.Value && u.LastLoginAt >= thirtyDaysAgo)
                .OrderByDescending(u => u.LastLoginAt)
                .Select(u => GetInitials(u.FullName ?? u.Email ?? "U"))
                .ToListAsync();

            var vm = new KnowledgeSharingViewModel
            {
                CanPost = canPost,
                CanAnnounce = canAnnounce,
                CanArchive = canArchive,
                SearchQuery = q,
                SelectedCategory = category,
                ArchiveStatus = archiveStatus,
                Discussions = discussions,
                Announcements = new List<KnowledgeAnnouncementItem>(),
                SharedDocuments = new List<KnowledgeSharedDocItem>(),
                ActiveMembers = activeMembersList,
                Categories = new List<string> { "All Categories", "General", "Health", "Environment", "Youth", "Education", "Governance", "Finance" },
                MembersOnline = activeMembersList.Count,
                TotalDiscussions = await _context.KnowledgeDiscussions.CountAsync(d => d.IsActive && !d.IsArchived && d.BarangayId == barangayId),
                ArchivedDiscussions = await _context.KnowledgeDiscussions.CountAsync(d => d.IsActive && d.IsArchived && d.BarangayId == barangayId),
                CurrentUserId = currentUserId,
                SuccessMessage = TempData["Success"] as string,
                ErrorMessage = TempData["Error"] as string
            };

            return View(vm);
        }

        private static string GetInitials(string fullName)
        {
            var parts = (fullName ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return "U";
            if (parts.Length == 1) return parts[0].Substring(0, 1).ToUpperInvariant();
            return (parts[0].Substring(0, 1) + parts[^1].Substring(0, 1)).ToUpperInvariant();
        }

        // POST: Create Discussion
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireActiveSubscription]
        public async Task<IActionResult> CreateDiscussion(string title, string content, string category)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            var role = HttpContext.Session.GetString("Role") ?? "";
            bool canPost = !string.IsNullOrEmpty(role); // All logged-in users can post
            if (!canPost) return RedirectToAction(nameof(KnowledgeSharing));

            title = (title ?? "").Trim();
            content = (content ?? "").Trim();
            category = string.IsNullOrWhiteSpace(category) ? "General" : category.Trim();

            // If no content provided, use title as content
            if (string.IsNullOrWhiteSpace(content)) content = title;

            if (string.IsNullOrWhiteSpace(title))
            {
                TempData["Error"] = "Title is required.";
                return RedirectToAction(nameof(KnowledgeSharing));
            }

            // Get author ID from session
            var userEmail = HttpContext.Session.GetString("UserName") ?? "";
            var authorId = await _context.BusinessUsers
                .Where(u => u.Email == userEmail)
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            if (authorId == 0) authorId = 1;

            var discussion = new KnowledgeDiscussion
            {
                Title = title,
                Content = content,
                Category = category,
                AuthorId = authorId,
                BarangayId = GetCurrentBarangayId(),
                IsActive = true,
                IsArchived = false,
                CreatedAt = DateTime.Now
            };

            _context.KnowledgeDiscussions.Add(discussion);
            await _context.SaveChangesAsync();
            await LogAuditAsync("Create", "KnowledgeSharing", discussion.Id, "Discussion", title, $"Created discussion: {title}");

            TempData["Success"] = $"Discussion \"{title}\" created successfully.";
            return RedirectToAction(nameof(KnowledgeSharing));
        }

        // POST: Edit Discussion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDiscussion(string id, string title, string content, string category)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            var role = HttpContext.Session.GetString("Role") ?? "";
            bool canEdit = !string.IsNullOrEmpty(role); // All logged-in users can edit their posts
            if (!canEdit) return RedirectToAction(nameof(KnowledgeSharing));

            if (!int.TryParse(id, out var discussionId))
            {
                TempData["Error"] = "Invalid discussion ID.";
                return RedirectToAction(nameof(KnowledgeSharing));
            }

            var discussion = await _context.KnowledgeDiscussions.FindAsync(discussionId);
            if (discussion == null || !discussion.IsActive)
            {
                TempData["Error"] = "Discussion not found.";
                return RedirectToAction(nameof(KnowledgeSharing));
            }

            // TENANT OWNERSHIP VALIDATION
            // STRICT TENANT VALIDATION
            if (discussion.BarangayId != GetCurrentBarangayId())
            {
                TempData["Error"] = "You cannot edit discussions from another barangay.";
                return RedirectToAction(nameof(KnowledgeSharing));
            }

            discussion.Title = (title ?? discussion.Title).Trim();
            discussion.Content = (content ?? discussion.Content).Trim();
            discussion.Category = string.IsNullOrWhiteSpace(category) ? discussion.Category : category.Trim();
            discussion.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            await LogAuditAsync("Edit", "KnowledgeSharing", discussion.Id, "Discussion", discussion.Title, $"Updated discussion: {discussion.Title}");

            TempData["Success"] = "Discussion updated successfully.";
            return RedirectToAction(nameof(KnowledgeSharing));
        }

        // POST: Archive Discussion
        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
        [Authorize(Roles = "super_admin,barangay_admin")]
        public async Task<IActionResult> ArchiveDiscussion(string id)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            var role = HttpContext.Session.GetString("Role") ?? "";
            if (role != "barangay_admin" && role != "super_admin")
                return RedirectToAction(nameof(KnowledgeSharing));

            if (int.TryParse(id, out var discussionId))
            {
                var discussion = await _context.KnowledgeDiscussions.FindAsync(discussionId);
                if (discussion != null)
                {
                    // TENANT OWNERSHIP VALIDATION
                    // STRICT TENANT VALIDATION
                    if (discussion.BarangayId != GetCurrentBarangayId())
                    {
                        TempData["Error"] = "You cannot archive discussions from another barangay.";
                        return RedirectToAction(nameof(KnowledgeSharing));
                    }

                    discussion.IsArchived = true;
                    discussion.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                    await LogAuditAsync("Archive", "KnowledgeSharing", discussion.Id, "Discussion", discussion.Title, $"Archived discussion: {discussion.Title}");
                    TempData["Success"] = "Discussion archived.";
                }
            }

            return RedirectToAction(nameof(KnowledgeSharing));
        }

        // POST: Restore Discussion
        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
        [Authorize(Roles = "super_admin,barangay_admin")]
        public async Task<IActionResult> RestoreDiscussion(string id)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            var role = HttpContext.Session.GetString("Role") ?? "";
            if (role != "barangay_admin" && role != "super_admin")
                return RedirectToAction(nameof(KnowledgeSharing));

            if (int.TryParse(id, out var discussionId))
            {
                var discussion = await _context.KnowledgeDiscussions.FindAsync(discussionId);
                if (discussion != null)
                {
                    // TENANT OWNERSHIP VALIDATION
                    // STRICT TENANT VALIDATION
                    if (discussion.BarangayId != GetCurrentBarangayId())
                    {
                        TempData["Error"] = "You cannot restore discussions from another barangay.";
                        return RedirectToAction(nameof(KnowledgeSharing));
                    }

                    discussion.IsArchived = false;
                    discussion.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Discussion restored.";
                }
            }

            return RedirectToAction(nameof(KnowledgeSharing), new { archiveStatus = "active" });
        }

        // POST: Delete Discussion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDiscussion(string id)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            if (int.TryParse(id, out var discussionId))
            {
                var discussion = await _context.KnowledgeDiscussions.FindAsync(discussionId);
                if (discussion != null)
                {
                    // STRICT TENANT VALIDATION
                    if (discussion.BarangayId != GetCurrentBarangayId())
                    {
                        TempData["Error"] = "You cannot delete discussions from another barangay.";
                        return RedirectToAction(nameof(KnowledgeSharing));
                    }

                    var title = discussion.Title;
                    discussion.IsActive = false;
                    discussion.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                    await LogAuditAsync("Delete", "KnowledgeSharing", discussion.Id, "Discussion", title, $"Deleted discussion: {title}");
                    TempData["Success"] = "Discussion deleted.";
                }
            }

            return RedirectToAction(nameof(KnowledgeSharing));
        }

        // POST: Quick Post (simplified create)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickPostKnowledge(string content)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            var role = HttpContext.Session.GetString("Role") ?? "";
            bool canPost = !string.IsNullOrEmpty(role); // All logged-in users can post
            if (!canPost) return RedirectToAction(nameof(KnowledgeSharing));

            content = (content ?? "").Trim();
            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["Error"] = "Content is required.";
                return RedirectToAction(nameof(KnowledgeSharing));
            }

            // Get author ID from session
            var userEmail = HttpContext.Session.GetString("UserName") ?? "";
            var authorId = await _context.BusinessUsers
                .Where(u => u.Email == userEmail)
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            if (authorId == 0) authorId = 1;

            // Create a quick discussion with auto-generated title
            var discussion = new KnowledgeDiscussion
            {
                Title = content.Length > 50 ? content.Substring(0, 50) + "..." : content,
                Content = content,
                Category = "General",
                AuthorId = authorId,
                BarangayId = GetCurrentBarangayId(),
                IsActive = true,
                IsArchived = false,
                CreatedAt = DateTime.Now
            };

            _context.KnowledgeDiscussions.Add(discussion);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Post created successfully.";
            return RedirectToAction(nameof(KnowledgeSharing));
        }

        // POST: Like/Unlike a discussion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LikeDiscussion(int id)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            var userIdStr = HttpContext.Session.GetString("UserId");
            if (!int.TryParse(userIdStr, out var userId) || userId == 0)
            {
                TempData["Error"] = "Unable to identify your user session.";
                return RedirectToAction(nameof(KnowledgeSharing));
            }

            var discussion = await _context.KnowledgeDiscussions.FindAsync(id);
            if (discussion == null || !discussion.IsActive)
            {
                TempData["Error"] = "Discussion not found.";
                return RedirectToAction(nameof(KnowledgeSharing));
            }

            // STRICT TENANT VALIDATION
            if (discussion.BarangayId != GetCurrentBarangayId())
            {
                TempData["Error"] = "You cannot like discussions from another barangay.";
                return RedirectToAction(nameof(KnowledgeSharing));
            }

            // Check if user already liked this discussion
            var existingLike = await _context.DiscussionLikes
                .FirstOrDefaultAsync(l => l.DiscussionId == id && l.UserId == userId);

            if (existingLike != null)
            {
                // Unlike: remove the like
                _context.DiscussionLikes.Remove(existingLike);
                discussion.LikesCount = Math.Max(0, discussion.LikesCount - 1);
            }
            else
            {
                // Like: add a new like
                _context.DiscussionLikes.Add(new DiscussionLike
                {
                    DiscussionId = id,
                    UserId = userId,
                    CreatedAt = DateTime.Now
                });
                discussion.LikesCount++;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(KnowledgeSharing));
        }

        // POST: Add a comment to a discussion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int discussionId, string content)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            var userIdStr = HttpContext.Session.GetString("UserId");
            if (!int.TryParse(userIdStr, out var userId) || userId == 0)
            {
                TempData["Error"] = "Unable to identify your user session.";
                return RedirectToAction(nameof(KnowledgeSharing));
            }

            content = (content ?? "").Trim();
            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["Error"] = "Comment content is required.";
                return RedirectToAction(nameof(KnowledgeSharing));
            }

            var discussion = await _context.KnowledgeDiscussions.FindAsync(discussionId);
            if (discussion == null || !discussion.IsActive)
            {
                TempData["Error"] = "Discussion not found.";
                return RedirectToAction(nameof(KnowledgeSharing));
            }

            // STRICT TENANT VALIDATION
            if (discussion.BarangayId != GetCurrentBarangayId())
            {
                TempData["Error"] = "You cannot comment on discussions from another barangay.";
                return RedirectToAction(nameof(KnowledgeSharing));
            }

            var comment = new DiscussionComment
            {
                DiscussionId = discussionId,
                AuthorId = userId,
                Content = content,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.DiscussionComments.Add(comment);
            discussion.RepliesCount++;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Comment added successfully.";
            return RedirectToAction(nameof(KnowledgeSharing));
        }

        public async Task<IActionResult> UserManagement()
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToDashboard();

            var role = GetCurrentRole();
            var barangayId = GetCurrentBarangayId();

            // Base query
            var userQuery = _context.BusinessUsers.AsQueryable();

            if (role == "super_admin")
            {
                // Super admin: only shows barangay admin accounts (not all users)
                userQuery = userQuery.Where(u => u.Role == "barangay_admin");
            }
            else if (role == "barangay_admin" && barangayId.HasValue)
            {
                // Barangay admin: only show users in their barangay, exclude super_admin
                userQuery = userQuery
                    .Where(u => u.BarangayId == barangayId.Value)
                    .Where(u => u.Role != "super_admin");
            }

            var dbUsers = await userQuery
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new
                {
                    u.Id,
                    u.FullName,
                    u.Email,
                    u.Role,
                    u.IsActive,
                    u.BarangayName
                })
                .ToListAsync();

            var users = dbUsers.Select(u => new UserItem
            {
                Id = u.Id.ToString(),
                Name = u.FullName,
                Email = u.Email,
                Role = Enum.TryParse<UserRole>(u.Role, out var r) ? r : UserRole.barangay_staff,
                Status = u.IsActive ? "active" : "inactive",
                Barangay = u.BarangayName ?? ""
            }).ToList();

            // Get list of barangays for dropdown
            List<string> barangays;
            if (role == "barangay_admin" && barangayId.HasValue)
            {
                // Barangay admin only sees their own barangay
                barangays = await _context.Barangays
                    .Where(b => b.IsActive && b.Id == barangayId.Value)
                    .Select(b => b.Name)
                    .ToListAsync();
            }
            else
            {
                barangays = await _context.Barangays
                    .Where(b => b.IsActive)
                    .OrderBy(b => b.Name)
                    .Select(b => b.Name)
                    .ToListAsync();
            }

            var vm = new UserManagementViewModel
            {
                Users = users,
                Barangays = barangays,
                CurrentUserRole = role,
                SuccessMessage = TempData["Success"] as string,
                ErrorMessage = TempData["Error"] as string
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "super_admin,barangay_admin")]
        public async Task<IActionResult> CreateUser(string name, string email, string password, string role, string barangay)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToDashboard();

            name = (name ?? "").Trim();
            email = (email ?? "").Trim();
            password = (password ?? "").Trim();
            role = (role ?? "barangay_staff").Trim();
            barangay = (barangay ?? "").Trim();

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
            {
                TempData["Error"] = "Name and email are required.";
                return RedirectToAction(nameof(UserManagement));
            }

            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            {
                TempData["Error"] = "Password must be at least 6 characters.";
                return RedirectToAction(nameof(UserManagement));
            }

            // Check if email already exists in BusinessUsers or Identity
            var existsBusiness = await _context.BusinessUsers.AnyAsync(u => u.Email == email);
            var existsIdentity = await _userManager.FindByEmailAsync(email);
            if (existsBusiness || existsIdentity != null)
            {
                TempData["Error"] = "A user with this email already exists.";
                return RedirectToAction(nameof(UserManagement));
            }

            // 1. Create Identity user (so they can log in)
            var identityUser = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var identityResult = await _userManager.CreateAsync(identityUser, password);
            if (!identityResult.Succeeded)
            {
                var errors = string.Join(", ", identityResult.Errors.Select(e => e.Description));
                TempData["Error"] = $"Failed to create user: {errors}";
                return RedirectToAction(nameof(UserManagement));
            }

            // 2. Assign role in Identity
            await _userManager.AddToRoleAsync(identityUser, role);

            // 3. Resolve BarangayId from name
            int? barangayId = null;
            if (!string.IsNullOrWhiteSpace(barangay))
            {
                var bgy = await _context.Barangays.FirstOrDefaultAsync(b => b.Name == barangay && b.IsActive);
                if (bgy != null) barangayId = bgy.Id;
            }

            // If barangay_admin creating a user, auto-assign their barangay
            if (!barangayId.HasValue && GetCurrentRole() == "barangay_admin")
            {
                barangayId = GetCurrentBarangayId();
                if (string.IsNullOrWhiteSpace(barangay))
                    barangay = HttpContext.Session.GetString("Barangay") ?? "";
            }

            // Enforce user limit based on subscription plan
            if (barangayId.HasValue)
            {
                var activeSub = await _context.BarangaySubscriptions
                    .Include(s => s.Plan)
                    .Where(s => s.IsActive && s.BarangayId == barangayId && s.Status == "Active" && s.EndDate >= DateTime.Today)
                    .OrderByDescending(s => s.EndDate)
                    .FirstOrDefaultAsync();

                if (activeSub?.Plan != null)
                {
                    var currentCount = await _context.BusinessUsers.CountAsync(u => u.IsActive && u.BarangayId == barangayId);
                    if (currentCount >= activeSub.Plan.UserLimit)
                    {
                        TempData["Error"] = $"User limit reached! Your {activeSub.Plan.Name} plan allows up to {activeSub.Plan.UserLimit} users. Please upgrade your plan to add more users.";
                        return RedirectToAction(nameof(UserManagement));
                    }
                }
            }

            // 4. Create BusinessUser record
            var user = new Models.Entities.User
            {
                FullName = name,
                Email = email,
                PasswordHash = "IDENTITY_MANAGED",
                Role = role,
                BarangayId = barangayId,
                BarangayName = barangay,
                IsActive = true,
                CreatedAt = DateTime.Now,
                CreatedBy = GetCurrentUserId()
            };

            _context.BusinessUsers.Add(user);
            await _context.SaveChangesAsync();

            // 5. Log the action
            await LogAuditAsync("Create", "UserManagement", user.Id, "User", name, $"Created user {email} with role {role}");

            TempData["Success"] = $"User \"{name}\" created successfully.";
            return RedirectToAction(nameof(UserManagement));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(string id, string name, string email, string role, string barangay)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToDashboard();

            if (!int.TryParse(id, out var userId))
            {
                TempData["Error"] = "Invalid user ID.";
                return RedirectToAction(nameof(UserManagement));
            }

            var user = await _context.BusinessUsers.FindAsync(userId);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(UserManagement));
            }

            user.FullName = (name ?? user.FullName).Trim();
            user.Email = (email ?? user.Email).Trim();
            user.Role = (role ?? user.Role).Trim();
            user.BarangayName = (barangay ?? "").Trim();
            user.UpdatedAt = DateTime.Now;

            // Sync role change to Identity
            var identityUser = await _userManager.FindByEmailAsync(user.Email);
            if (identityUser != null)
            {
                var currentRoles = await _userManager.GetRolesAsync(identityUser);
                if (currentRoles.Any()) await _userManager.RemoveFromRolesAsync(identityUser, currentRoles);
                await _userManager.AddToRoleAsync(identityUser, user.Role);
            }

            await _context.SaveChangesAsync();
            await LogAuditAsync("Edit", "UserManagement", user.Id, "User", user.FullName, $"Updated user: {user.Email}");

            TempData["Success"] = $"User \"{user.FullName}\" updated successfully.";
            return RedirectToAction(nameof(UserManagement));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
        [Authorize(Roles = "super_admin,barangay_admin")]
        public async Task<IActionResult> ArchiveUser(string id)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToDashboard();

            if (int.TryParse(id, out var userId))
            {
                var user = await _context.BusinessUsers.FindAsync(userId);
                if (user != null)
                {
                    user.IsActive = false;
                    user.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                    await LogAuditAsync("Archive", "UserManagement", user.Id, "User", user.FullName, $"Archived user: {user.Email}");
                    TempData["Success"] = $"User \"{user.FullName}\" archived.";
                }
            }

            return RedirectToAction(nameof(UserManagement));
        }

        // POST: /Home/ToggleUserStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
        [Authorize(Roles = "super_admin,barangay_admin")]
        public async Task<IActionResult> ToggleUserStatus(string id)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToDashboard();

            if (int.TryParse(id, out var userId))
            {
                var user = await _context.BusinessUsers.FindAsync(userId);
                if (user != null)
                {
                    user.IsActive = !user.IsActive;
                    user.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                    var newStatus = user.IsActive ? "Active" : "Inactive";
                    await LogAuditAsync("Updated", "UserManagement", user.Id, "User", user.FullName,
                        $"Toggled status to {newStatus}: {user.Email}");
                    TempData["Success"] = $"User \"{user.FullName}\" status changed to {newStatus}.";
                }
            }

            return RedirectToAction(nameof(UserManagement));
        }

        // GET: /Home/Announcements
        [HttpGet]
        public async Task<IActionResult> Announcements(string filter = "all", string archiveStatus = "active")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            // MULTI-TENANT ISOLATION: Super_admin cannot access barangay internal data
            if (IsSuperAdmin()) return RedirectToAction("System", "Dashboard");

            var role = HttpContext.Session.GetString("Role") ?? "";
            var barangayId = GetCurrentBarangayId();
            
            if (!barangayId.HasValue)
            {
                TempData["Error"] = "No barangay assigned to your account.";
                return RedirectToAction("Barangay", "Dashboard");
            }

            bool canCreate = role == "barangay_admin" || role == "barangay_secretary";
            bool canEdit = role == "barangay_admin" || role == "barangay_secretary";
            bool canArchive = role == "barangay_admin" || role == "super_admin";

            filter = (filter ?? "all").Trim().ToLower();
            archiveStatus = (archiveStatus ?? "active").Trim().ToLower();

            // ERP Rule: Only admin roles can view archived records
            if (!canArchive) archiveStatus = "active";

            // Council members can only see published announcements
            if (role == "council_member")
                filter = "published";

            // STRICT TENANT FILTERING: Query announcements from user's barangay only
            var query = _context.Announcements
                .Where(a => a.IsActive)
                .Where(a => a.BarangayId == barangayId.Value)
                .Include(a => a.Author)
                .AsQueryable();

            // Filter by archive status
            if (archiveStatus == "active")
                query = query.Where(a => !a.IsArchived);
            else if (archiveStatus == "archived")
                query = query.Where(a => a.IsArchived);
            // "all" shows everything

            if (filter != "all")
                query = query.Where(a => a.Status.ToLower() == filter);

            var announcements = await query
                .OrderByDescending(a => a.IsPinned)
                .ThenByDescending(a => a.CreatedAt)
                .Select(a => new AnnouncementItem
                {
                    Id = a.Id.ToString(),
                    Title = a.Title,
                    Content = a.Content,
                    Priority = a.Priority,
                    Status = a.Status,
                    Date = a.CreatedAt.ToString("yyyy-MM-dd"),
                    Author = a.Author != null ? a.Author.FullName : "Unknown",
                    Views = a.ViewCount,
                    Pinned = a.IsPinned,
                    IsArchived = a.IsArchived
                })
                .ToListAsync();

            // Get counts from all active announcements for this barangay (strict tenant filtering)
            var allAnnouncements = await _context.Announcements
                .Where(a => a.IsActive)
                .Where(a => a.BarangayId == barangayId.Value)
                .ToListAsync();

            var vm = new AnnouncementsViewModel
            {
                Filter = filter,
                ArchiveStatus = archiveStatus,
                CanCreate = canCreate,
                CanEdit = canEdit,
                CanArchive = canArchive,
                Announcements = announcements,

                Total = allAnnouncements.Count(x => !x.IsArchived),
                Published = allAnnouncements.Count(x => !x.IsArchived && x.Status == "published"),
                Drafts = allAnnouncements.Count(x => !x.IsArchived && x.Status == "draft"),
                Pinned = allAnnouncements.Count(x => !x.IsArchived && x.IsPinned),
                Archived = allAnnouncements.Count(x => x.IsArchived),

                SuccessMessage = TempData["Success"] as string,
                ErrorMessage = TempData["Error"] as string
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
        [RequireActiveSubscription]
        [Authorize(Roles = "super_admin,barangay_admin,barangay_secretary")]
        public async Task<IActionResult> CreateAnnouncement(string title, string content, string priority, string status, string filter = "all")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            var role = HttpContext.Session.GetString("Role") ?? "";
            var canCreate = role == "barangay_admin" || role == "barangay_secretary" || role == "super_admin";
            if (!canCreate) return RedirectToAction(nameof(Announcements), new { filter });

            title = (title ?? "").Trim();
            content = (content ?? "").Trim();
            priority = string.IsNullOrWhiteSpace(priority) ? "medium" : priority.Trim().ToLower();
            status = string.IsNullOrWhiteSpace(status) ? "draft" : status.Trim().ToLower();

            if (string.IsNullOrWhiteSpace(title))
            {
                TempData["Error"] = "Title is required.";
                return RedirectToAction(nameof(Announcements), new { filter });
            }

            // Get author ID from session
            var userEmail = HttpContext.Session.GetString("UserName") ?? "";
            var authorId = await _context.BusinessUsers
                .Where(u => u.Email == userEmail)
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            if (authorId == 0) authorId = 1;

            var announcement = new Announcement
            {
                Title = title,
                Content = content,
                Priority = priority,
                Status = status,
                AuthorId = authorId,
                BarangayId = GetCurrentBarangayId(),
                IsPinned = false,
                IsActive = true,
                IsArchived = false,
                PublishedAt = status == "published" ? DateTime.Now : null,
                CreatedAt = DateTime.Now
            };

            _context.Announcements.Add(announcement);
            await _context.SaveChangesAsync();

            // Send real-time notification to all barangay users if published
            if (status == "published" && announcement.BarangayId.HasValue)
            {
                await _notificationService.NotifyNewAnnouncement(
                    announcement.BarangayId.Value, 
                    title, 
                    priority, 
                    userEmail);
            }

            await LogAuditAsync("Create", "Announcements", announcement.Id, "Announcement", title, $"Created announcement: {title}");

            TempData["Success"] = $"Announcement \"{title}\" created successfully.";
            return RedirectToAction(nameof(Announcements), new { filter });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
        public async Task<IActionResult> EditAnnouncement(string id, string title, string content, string priority, string status, string filter = "all")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            var role = HttpContext.Session.GetString("Role") ?? "";
            var canEdit = role == "barangay_admin" || role == "barangay_secretary";
            if (!canEdit) return RedirectToAction(nameof(Announcements), new { filter });

            if (!int.TryParse(id, out var announcementId))
            {
                TempData["Error"] = "Invalid announcement ID.";
                return RedirectToAction(nameof(Announcements), new { filter });
            }

            var announcement = await _context.Announcements.FindAsync(announcementId);
            if (announcement == null || !announcement.IsActive)
            {
                TempData["Error"] = "Announcement not found.";
                return RedirectToAction(nameof(Announcements), new { filter });
            }

            // STRICT TENANT VALIDATION
            if (announcement.BarangayId != GetCurrentBarangayId())
            {
                TempData["Error"] = "You cannot edit announcements from another barangay.";
                return RedirectToAction(nameof(Announcements), new { filter });
            }

            announcement.Title = (title ?? announcement.Title).Trim();
            announcement.Content = (content ?? "").Trim();
            announcement.Priority = string.IsNullOrWhiteSpace(priority) ? announcement.Priority : priority.Trim().ToLower();
            
            // If changing to published, set PublishedAt
            var wasPublished = status == "published" && announcement.Status != "published";
            if (wasPublished)
            {
                announcement.PublishedAt = DateTime.Now;
            }
            announcement.Status = string.IsNullOrWhiteSpace(status) ? announcement.Status : status.Trim().ToLower();
            announcement.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            // Send real-time notification if just published
            if (wasPublished && announcement.BarangayId.HasValue)
            {
                var authorName = HttpContext.Session.GetString("UserName") ?? "Unknown";
                await _notificationService.NotifyNewAnnouncement(
                    announcement.BarangayId.Value,
                    announcement.Title,
                    announcement.Priority,
                    authorName);
            }

            await LogAuditAsync("Edit", "Announcements", announcementId, "Announcement", announcement.Title, $"Updated announcement: {announcement.Title}");

            TempData["Success"] = "Announcement updated successfully.";
            return RedirectToAction(nameof(Announcements), new { filter });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
        [Authorize(Roles = "super_admin,barangay_admin")]
        public async Task<IActionResult> ArchiveAnnouncement(string id, string filter = "all")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            var role = HttpContext.Session.GetString("Role") ?? "";
            if (role != "barangay_admin" && role != "super_admin")
                return RedirectToAction(nameof(Announcements), new { filter });

            if (int.TryParse(id, out var announcementId))
            {
                var announcement = await _context.Announcements.FindAsync(announcementId);
                if (announcement != null)
                {
                    // STRICT TENANT VALIDATION
                    if (announcement.BarangayId != GetCurrentBarangayId())
                    {
                        TempData["Error"] = "You cannot archive announcements from another barangay.";
                        return RedirectToAction(nameof(Announcements), new { filter });
                    }

                    announcement.IsArchived = true;
                    announcement.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                    await LogAuditAsync("Archive", "Announcements", announcement.Id, "Announcement", announcement.Title, $"Archived announcement: {announcement.Title}");
                }
            }

            TempData["Success"] = "Announcement archived.";
            return RedirectToAction(nameof(Announcements), new { filter });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
        [Authorize(Roles = "super_admin,barangay_admin")]
        public async Task<IActionResult> RestoreAnnouncement(string id, string filter = "all")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            var role = HttpContext.Session.GetString("Role") ?? "";
            if (role != "barangay_admin" && role != "super_admin")
                return RedirectToAction(nameof(Announcements), new { filter });

            if (int.TryParse(id, out var announcementId))
            {
                var announcement = await _context.Announcements.FindAsync(announcementId);
                if (announcement != null)
                {
                    // STRICT TENANT VALIDATION
                    if (announcement.BarangayId != GetCurrentBarangayId())
                    {
                        TempData["Error"] = "You cannot restore announcements from another barangay.";
                        return RedirectToAction(nameof(Announcements), new { filter });
                    }

                    announcement.IsArchived = false;
                    announcement.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                    await LogAuditAsync("Restore", "Announcements", announcement.Id, "Announcement", announcement.Title, $"Restored announcement: {announcement.Title}");
                }
            }

            TempData["Success"] = "Announcement restored.";
            return RedirectToAction(nameof(Announcements), new { filter, archiveStatus = "active" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePinAnnouncement(string id, string filter = "all")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            var role = HttpContext.Session.GetString("Role") ?? "";
            var canPin = role == "barangay_admin";
            if (!canPin) return RedirectToAction(nameof(Announcements), new { filter });

            if (int.TryParse(id, out var announcementId))
            {
                var announcement = await _context.Announcements.FindAsync(announcementId);
                if (announcement != null && announcement.IsActive && !announcement.IsArchived)
                {
                    announcement.IsPinned = !announcement.IsPinned;
                    announcement.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                    TempData["Success"] = announcement.IsPinned ? "Announcement pinned." : "Announcement unpinned.";
                }
            }

            return RedirectToAction(nameof(Announcements), new { filter });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IncrementAnnouncementViews(string id, string filter = "all")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            if (int.TryParse(id, out var announcementId))
            {
                var announcement = await _context.Announcements.FindAsync(announcementId);
                if (announcement != null && announcement.IsActive)
                {
                    announcement.ViewCount++;
                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToAction(nameof(Announcements), new { filter });
        }

        // GET: /Home/AuditLogs
        [HttpGet]
        public async Task<IActionResult> AuditLogs(string q = "", string module = "all", string action = "all")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToDashboard();

            q = (q ?? "").Trim().ToLower();
            module = (module ?? "all").Trim();
            action = (action ?? "all").Trim();

            var role = GetCurrentRole();
            var barangayId = GetCurrentBarangayId();

            // Build query based on role
            IQueryable<AuditLog> logQuery = _context.AuditLogs.Where(l => l.IsActive);

            // Super admin sees ALL logs, barangay roles see only their barangay
            if (role != "super_admin" && barangayId.HasValue)
            {
                logQuery = logQuery.Where(l => l.BarangayId == barangayId.Value);
            }
            else if (role != "super_admin" && !barangayId.HasValue)
            {
                logQuery = logQuery.Where(l => false);
            }

            // Get logs and convert to LogItem
            var rawLogs = await logQuery.OrderByDescending(l => l.CreatedAt).ToListAsync();

            var list = rawLogs.Select(l => new LogItem
            {
                Id = l.Id.ToString(),
                Timestamp = l.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                User = l.UserName ?? l.UserEmail ?? "System",
                Action = l.Action ?? "",
                Module = l.Module ?? "",
                Target = l.TargetName ?? l.TargetType ?? "",
                Ip = l.IpAddress ?? ""
            }).ToList();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(q))
            {
                list = list.Where(l =>
                    (l.User ?? "").ToLower().Contains(q) ||
                    (l.Target ?? "").ToLower().Contains(q) ||
                    (l.Action ?? "").ToLower().Contains(q)
                ).ToList();
            }

            // Apply module filter
            if (module != "all")
            {
                list = list.Where(l => l.Module == module).ToList();
            }

            // Apply action filter
            if (action != "all")
            {
                list = list.Where(l => l.Action == action).ToList();
            }

            var vm = new AuditLogsViewModel
            {
                SearchQuery = q,
                ModuleFilter = module,
                ActionFilter = action,
                Logs = list
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
        [Authorize(Roles = "super_admin,barangay_admin")]
        public async Task<IActionResult> ArchiveLog(string id, string q = "", string module = "all", string action = "all")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToDashboard();

            if (long.TryParse(id, out var logId))
            {
                var log = await _context.AuditLogs.FindAsync(logId);
                if (log != null)
                {
                    log.IsActive = false;
                    await _context.SaveChangesAsync();
                }
            }

            TempData["Success"] = "Log entry archived.";
            return RedirectToAction(nameof(AuditLogs), new { q, module, action });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
        [Authorize(Roles = "super_admin")]
        public async Task<IActionResult> ClearAllLogs()
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsSuperAdmin()) return RedirectToDashboard();

            var logs = _context.AuditLogs.Where(l => l.IsActive);
            await logs.ForEachAsync(l => l.IsActive = false);
            await _context.SaveChangesAsync();
            await LogAuditAsync("ClearAll", "AuditLogs", null, "AuditLog", null, "Cleared all audit logs");

            TempData["Success"] = "All logs cleared.";
            return RedirectToAction(nameof(AuditLogs));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
        [Authorize(Roles = "super_admin,barangay_admin")]
        public async Task<IActionResult> ExportLogsCsv(string q = "", string module = "all")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToDashboard();

            q = (q ?? "").Trim().ToLower();
            module = (module ?? "all").Trim();

            var role = GetCurrentRole();
            var barangayId = GetCurrentBarangayId();

            // STRICT TENANT ISOLATION: same as AuditLogs GET
            var logQuery = _context.AuditLogs.Where(l => l.IsActive);
            if (role == "super_admin")
            {
                // Super admin sees ALL logs (no additional filter)
            }
            else if (barangayId.HasValue)
                logQuery = logQuery.Where(l => l.BarangayId == barangayId.Value);
            else
                logQuery = logQuery.Where(l => false);

            // First materialize from database, then project (avoids EF Core translation issues)
            var rawLogs = await logQuery
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            var allLogs = rawLogs.Select(l => new LogItem
            {
                Id = l.Id.ToString(),
                Timestamp = l.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                User = l.UserName ?? l.UserEmail ?? "System",
                Action = l.Action ?? "",
                Module = l.Module ?? "",
                Target = l.TargetName ?? l.TargetType ?? "",
                Ip = l.IpAddress ?? ""
            }).ToList();

            var list = allLogs.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                list = list.Where(l =>
                    (l.User ?? "").ToLower().Contains(q) ||
                    (l.Target ?? "").ToLower().Contains(q) ||
                    (l.Action ?? "").ToLower().Contains(q)
                );
            }

            if (module != "all")
            {
                list = list.Where(l => l.Module == module);
            }

            var sb = new StringBuilder();
            sb.AppendLine("Timestamp,User,Action,Module,Target,IP");

            foreach (var l in list.OrderByDescending(x => x.Timestamp))
            {
                string Esc(string s) => "\"" + (s ?? "").Replace("\"", "\"\"") + "\"";
                sb.AppendLine(string.Join(",",
                    Esc(l.Timestamp),
                    Esc(l.User),
                    Esc(l.Action),
                    Esc(l.Module),
                    Esc(l.Target),
                    Esc(l.Ip)
                ));
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", "audit_logs.csv");
        }

        // GET: /Home/Settings
        [HttpGet]
        public async Task<IActionResult> Settings(string tab = "general")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToDashboard();

            tab = (tab ?? "general").Trim().ToLower();

            // Load user profile from DB
            var userId = GetCurrentUserId();
            var user = userId.HasValue ? await _context.BusinessUsers.FindAsync(userId.Value) : null;

            var vm = new SettingsViewModel
            {
                Tab = tab,

                FullName = user?.FullName ?? HttpContext.Session.GetString("UserName") ?? "",
                Email = user?.Email ?? HttpContext.Session.GetString("UserName") ?? "",
                Barangay = user?.BarangayName ?? HttpContext.Session.GetString("Barangay") ?? "",
                Language = HttpContext.Session.GetString("Settings_Language") ?? "en",

                NotifApprovals = (HttpContext.Session.GetString("Settings_NotifApprovals") ?? "true") == "true",
                NotifPolicyUpdates = (HttpContext.Session.GetString("Settings_NotifPolicyUpdates") ?? "true") == "true",
                NotifSubmissions = (HttpContext.Session.GetString("Settings_NotifSubmissions") ?? "true") == "true",
                NotifAnnouncements = (HttpContext.Session.GetString("Settings_NotifAnnouncements") ?? "false") == "true",
                NotifReplies = (HttpContext.Session.GetString("Settings_NotifReplies") ?? "false") == "true",

                TwoFaEnabled = (HttpContext.Session.GetString("Settings_TwoFa") ?? "false") == "true",

                MaintenanceMode = (HttpContext.Session.GetString("Settings_Maintenance") ?? "false") == "true",
                SessionTimeout = HttpContext.Session.GetString("Settings_SessionTimeout") ?? "30",
                DocFormat = HttpContext.Session.GetString("Settings_DocFormat") ?? "pdf"
            };

            vm.SuccessMessage = TempData["Success"] as string;
            vm.ErrorMessage = TempData["Error"] as string;

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveProfile(SettingsViewModel model)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToDashboard();

            if (string.IsNullOrWhiteSpace(model.FullName))
            {
                TempData["Error"] = "Full name is required.";
                return RedirectToAction(nameof(Settings), new { tab = "general" });
            }

            var userId = GetCurrentUserId();
            if (userId.HasValue)
            {
                var user = await _context.BusinessUsers.FindAsync(userId.Value);
                if (user != null)
                {
                    user.FullName = (model.FullName ?? user.FullName).Trim();
                    user.BarangayName = (model.Barangay ?? "").Trim();
                    user.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();

                    // Update session to reflect changes
                    HttpContext.Session.SetString("UserName", user.Email);
                    if (!string.IsNullOrWhiteSpace(user.BarangayName))
                        HttpContext.Session.SetString("Barangay", user.BarangayName);

                    await LogAuditAsync("Edit", "Settings", user.Id, "User", user.FullName, "Updated profile settings");
                    _logger.LogInformation("Profile updated for user {UserId}: {FullName}", user.Id, user.FullName);
                }
            }

            HttpContext.Session.SetString("Settings_Language", (model.Language ?? "en").Trim());

            TempData["Success"] = "Profile saved. Your profile information has been updated.";
            return RedirectToAction(nameof(Settings), new { tab = "general" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveNotifications(SettingsViewModel model)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToDashboard();

            HttpContext.Session.SetString("Settings_NotifApprovals", model.NotifApprovals ? "true" : "false");
            HttpContext.Session.SetString("Settings_NotifPolicyUpdates", model.NotifPolicyUpdates ? "true" : "false");
            HttpContext.Session.SetString("Settings_NotifSubmissions", model.NotifSubmissions ? "true" : "false");
            HttpContext.Session.SetString("Settings_NotifAnnouncements", model.NotifAnnouncements ? "true" : "false");
            HttpContext.Session.SetString("Settings_NotifReplies", model.NotifReplies ? "true" : "false");

            TempData["Success"] = "Preferences saved. Notification preferences updated.";
            return RedirectToAction(nameof(Settings), new { tab = "notifications" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToDashboard();

            if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
            {
                TempData["Error"] = "Please enter current and new password.";
                return RedirectToAction(nameof(Settings), new { tab = "security" });
            }

            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "New passwords do not match.";
                return RedirectToAction(nameof(Settings), new { tab = "security" });
            }

            if (newPassword.Length < 6)
            {
                TempData["Error"] = "New password must be at least 6 characters.";
                return RedirectToAction(nameof(Settings), new { tab = "security" });
            }

            // Get the Identity user
            var email = HttpContext.Session.GetString("UserName") ?? "";
            var identityUser = await _userManager.FindByEmailAsync(email);
            if (identityUser == null)
            {
                TempData["Error"] = "User account not found.";
                return RedirectToAction(nameof(Settings), new { tab = "security" });
            }

            // Verify current password and change to new
            var changeResult = await _userManager.ChangePasswordAsync(identityUser, currentPassword, newPassword);
            if (!changeResult.Succeeded)
            {
                var errors = string.Join(", ", changeResult.Errors.Select(e => e.Description));
                TempData["Error"] = $"Password change failed: {errors}";
                return RedirectToAction(nameof(Settings), new { tab = "security" });
            }

            await LogAuditAsync("PasswordChange", "Settings", GetCurrentUserId(), "User", email, "Changed password");
            _logger.LogInformation("Password changed for user: {Email}", email);

            TempData["Success"] = "Password updated. Your password has been changed.";
            return RedirectToAction(nameof(Settings), new { tab = "security" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "super_admin")]
        public async Task<IActionResult> ToggleTwoFa(bool twoFaEnabled)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsSuperAdmin()) return RedirectToDashboard();

            HttpContext.Session.SetString("Settings_TwoFa", twoFaEnabled ? "true" : "false");
            await LogAuditAsync("Update", "SecuritySettings", null, "System", "TwoFA", twoFaEnabled ? "Enabled 2FA in settings." : "Disabled 2FA in settings.");
            TempData["Success"] = twoFaEnabled ? "2FA Enabled." : "2FA Disabled.";
            return RedirectToAction(nameof(Settings), new { tab = "security" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "super_admin")]
        public async Task<IActionResult> SaveSystem(SettingsViewModel model)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsSuperAdmin()) return RedirectToDashboard();

            HttpContext.Session.SetString("Settings_Maintenance", model.MaintenanceMode ? "true" : "false");
            HttpContext.Session.SetString("Settings_SessionTimeout", model.SessionTimeout ?? "30");
            HttpContext.Session.SetString("Settings_DocFormat", model.DocFormat ?? "pdf");

            await LogAuditAsync("Update", "SystemSettings", null, "System", "GlobalSettings",
                $"Updated system settings: Maintenance={model.MaintenanceMode}, SessionTimeout={model.SessionTimeout}, DocFormat={model.DocFormat}");
            TempData["Success"] = "System settings saved. System preferences have been updated.";
            return RedirectToAction(nameof(Settings), new { tab = "system" });
        }

        [HttpGet]
        [Authorize(Roles = "super_admin")]
        public async Task<IActionResult> PasswordRequests(string statusFilter = "all")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsSuperAdmin()) return RedirectToDashboard();

            await LogAuditAsync("View", "PasswordRequests", null, "PasswordReset", "Requests", "Viewed password reset requests queue.");

            statusFilter = (statusFilter ?? "all").Trim();

            var query = _context.PasswordResetRequests
                .Where(r => r.IsActive)
                .Include(r => r.User)
                .AsQueryable();

            if (statusFilter != "all")
                query = query.Where(r => r.Status == statusFilter);

            var requests = await query
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new PasswordResetRequestViewModel
                {
                    Id = r.Id,
                    Email = r.Email,
                    UserName = r.User != null ? r.User.FullName : "Unknown",
                    Status = r.Status,
                    CreatedAt = r.CreatedAt,
                    ProcessedAt = r.ProcessedAt,
                    Notes = r.Notes ?? ""
                })
                .ToListAsync();

            ViewBag.Requests = requests;
            ViewBag.StatusFilter = statusFilter;
            ViewBag.TotalPending = await _context.PasswordResetRequests.CountAsync(r => r.IsActive && r.Status == "Pending");
            ViewBag.TotalProcessed = await _context.PasswordResetRequests.CountAsync(r => r.IsActive && r.Status != "Pending");
            ViewBag.SuccessMessage = TempData["Success"];
            ViewBag.ErrorMessage = TempData["Error"];

            return View();
        }

        // POST: Process password reset request (approve = reset password)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "super_admin")]
        public async Task<IActionResult> ProcessPasswordRequest(int id, string action, string newPassword = "")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsSuperAdmin()) return RedirectToDashboard();

            var request = await _context.PasswordResetRequests
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id && r.IsActive);

            if (request == null)
            {
                TempData["Error"] = "Request not found.";
                return RedirectToAction(nameof(PasswordRequests));
            }

            if (action == "approve")
            {
                // Reset the user's password via Identity
                var identityUser = await _userManager.FindByEmailAsync(request.Email);
                if (identityUser != null)
                {
                    if (string.IsNullOrWhiteSpace(newPassword))
                    {
                        TempData["Error"] = "A new password is required to approve this request.";
                        return RedirectToAction(nameof(PasswordRequests));
                    }

                    var tempPassword = newPassword;
                    var token = await _userManager.GeneratePasswordResetTokenAsync(identityUser);
                    var resetResult = await _userManager.ResetPasswordAsync(identityUser, token, tempPassword);

                    if (resetResult.Succeeded)
                    {
                        request.Status = "Approved";
                        request.ProcessedAt = DateTime.Now;
                        request.ProcessedById = GetCurrentUserId();
                        request.Notes = $"Password reset to temporary password. User must change on next login.";
                        await _context.SaveChangesAsync();
                        await LogAuditAsync("Approve", "PasswordRequests", request.Id, "PasswordReset", request.Email, $"Approved password reset for {request.Email}");
                        TempData["Success"] = $"Password reset for {request.Email}. Temporary password set.";
                    }
                    else
                    {
                        var errors = string.Join(", ", resetResult.Errors.Select(e => e.Description));
                        TempData["Error"] = $"Failed to reset password: {errors}";
                    }
                }
                else
                {
                    TempData["Error"] = "Identity user not found for this email.";
                }
            }
            else if (action == "reject")
            {
                request.Status = "Rejected";
                request.ProcessedAt = DateTime.Now;
                request.ProcessedById = GetCurrentUserId();
                request.Notes = "Request rejected by administrator.";
                await _context.SaveChangesAsync();
                await LogAuditAsync("Reject", "PasswordRequests", request.Id, "PasswordReset", request.Email, $"Rejected password reset for {request.Email}");
                TempData["Success"] = $"Password reset request for {request.Email} rejected.";
            }

            return RedirectToAction(nameof(PasswordRequests));
        }

        // GET: /Home/ForgotPassword
        [AllowAnonymous]
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            SetRecaptchaSiteKey();
            return View(new ForgotPasswordViewModel());
        }

        // POST: /Home/ForgotPassword
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Submitted = false;
                SetRecaptchaSiteKey();
                return View(model);
            }

            if (!await IsRecaptchaValidAsync(model.RecaptchaToken))
            {
                model.Submitted = false;
                model.ErrorMessage = "Security verification failed. Please complete CAPTCHA and try again.";
                SetRecaptchaSiteKey();
                return View(model);
            }

            var email = (model.Email ?? "").Trim().ToLower();

            // Always return the same success response to prevent user enumeration.
            var identityUser = await _userManager.FindByEmailAsync(email);
            if (identityUser != null)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(identityUser);
                var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
                var tokenHash = HashResetToken(encodedToken);
                var expiresAtUtc = DateTime.UtcNow.AddHours(1);

                var pendingRequests = await _context.PasswordResetRequests
                    .Where(r => r.IsActive && r.Email.ToLower() == email && r.Status == "Pending")
                    .ToListAsync();

                foreach (var pending in pendingRequests)
                {
                    pending.Status = "Expired";
                    pending.IsActive = false;
                    pending.ProcessedAt = DateTime.UtcNow;
                    pending.Notes = "Superseded by a newer password reset request.";
                }

                var businessUser = await _context.BusinessUsers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.IsActive && u.Email.ToLower() == email);

                var resetRequest = new PasswordResetRequest
                {
                    UserId = businessUser?.Id,
                    Email = email,
                    Token = tokenHash,
                    Status = "Pending",
                    ExpiresAt = expiresAtUtc,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    Notes = "Self-service forgot-password request."
                };

                _context.PasswordResetRequests.Add(resetRequest);
                await _context.SaveChangesAsync();

                var callbackUrl = Url.Action(
                    nameof(ResetPassword),
                    "Home",
                    new { token = encodedToken, email = identityUser.Email },
                    protocol: Request.Scheme);

                if (!string.IsNullOrWhiteSpace(callbackUrl))
                {
                    var body = $"Please reset your password by clicking this link: <a href='{callbackUrl}'>Reset Password</a>";
                    try
                    {
                        await _emailSender.SendEmailAsync(identityUser.Email!, "Reset your JAS-MINE password", body);
                        await LogAuditAsync("Create", "PasswordRequests", resetRequest.Id, "PasswordReset", identityUser.Email, "Self-service password reset link issued.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send password reset email to {Email}", identityUser.Email);
                    }
                }
            }

            model.Submitted = true;
            model.SuccessMessage = "If your email is registered, a password reset link has been sent.";

            return View(model);
        }

        // GET: /Home/ResetPassword
        [AllowAnonymous]
        [HttpGet]
        public IActionResult ResetPassword(string? token, string? email)
        {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(email))
            {
                return View(new ResetPasswordViewModel
                {
                    ErrorMessage = "Invalid password reset link."
                });
            }

            var emailValue = email.Trim().ToLower();
            var tokenHash = HashResetToken(token);

            var resetRequest = _context.PasswordResetRequests
                .FirstOrDefault(r => r.IsActive
                    && r.Status == "Pending"
                    && r.Email.ToLower() == emailValue
                    && r.Token == tokenHash);

            if (resetRequest == null)
            {
                return View(new ResetPasswordViewModel
                {
                    ErrorMessage = "This reset link is invalid, expired, or has already been used."
                });
            }

            if (resetRequest.ExpiresAt.HasValue && resetRequest.ExpiresAt.Value <= DateTime.UtcNow)
            {
                resetRequest.Status = "Expired";
                resetRequest.IsActive = false;
                resetRequest.ProcessedAt = DateTime.UtcNow;
                resetRequest.Notes = "Reset link expired before use.";
                _context.SaveChanges();

                return View(new ResetPasswordViewModel
                {
                    ErrorMessage = "This reset link has expired. Please request a new one."
                });
            }

            return View(new ResetPasswordViewModel
            {
                Email = email,
                Token = token
            });
        }

        // POST: /Home/ResetPassword
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var emailValue = (model.Email ?? "").Trim().ToLower();
            var tokenHash = HashResetToken(model.Token ?? string.Empty);

            var resetRequest = await _context.PasswordResetRequests
                .FirstOrDefaultAsync(r => r.IsActive
                    && r.Status == "Pending"
                    && r.Email.ToLower() == emailValue
                    && r.Token == tokenHash);

            if (resetRequest == null)
            {
                model.ErrorMessage = "This reset link is invalid, expired, or has already been used.";
                return View(model);
            }

            if (resetRequest.ExpiresAt.HasValue && resetRequest.ExpiresAt.Value <= DateTime.UtcNow)
            {
                resetRequest.Status = "Expired";
                resetRequest.IsActive = false;
                resetRequest.ProcessedAt = DateTime.UtcNow;
                resetRequest.Notes = "Reset link expired before submission.";
                await _context.SaveChangesAsync();

                model.ErrorMessage = "This reset link has expired. Please request a new one.";
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(emailValue);
            if (user == null)
            {
                resetRequest.Status = "Expired";
                resetRequest.IsActive = false;
                resetRequest.ProcessedAt = DateTime.UtcNow;
                resetRequest.Notes = "Identity user not found while processing reset.";
                await _context.SaveChangesAsync();

                model.ErrorMessage = "This reset link is invalid, expired, or has already been used.";
                return View(model);
            }

            string decodedToken;
            try
            {
                decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Token ?? ""));
            }
            catch
            {
                model.ErrorMessage = "Invalid or expired reset token.";
                return View(model);
            }

            var result = await _userManager.ResetPasswordAsync(user, decodedToken, model.Password);
            if (result.Succeeded)
            {
                resetRequest.Status = "Completed";
                resetRequest.IsActive = false;
                resetRequest.ProcessedAt = DateTime.UtcNow;
                resetRequest.Notes = "Password reset completed by account owner.";
                await _context.SaveChangesAsync();

                model.Submitted = true;
                model.SuccessMessage = "Your password has been reset successfully. You may now log in.";
                await LogAuditAsync("Complete", "PasswordRequests", resetRequest.Id, "PasswordReset", user.Email, "Password reset completed via secure link.");
                return View(model);
            }

            var invalidToken = result.Errors.Any(e => e.Code.Contains("InvalidToken", StringComparison.OrdinalIgnoreCase));
            if (invalidToken)
            {
                resetRequest.Status = "Expired";
                resetRequest.IsActive = false;
                resetRequest.ProcessedAt = DateTime.UtcNow;
                resetRequest.Notes = "Identity rejected reset token as invalid/used.";
                await _context.SaveChangesAsync();
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            model.ErrorMessage = "Password reset failed. The token may be invalid or expired.";
            return View(model);
        }

        // ✅ UPDATED: /Home/Logout (clears session + identity cookie)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            _logger.LogInformation("Logout for user {User}", User?.Identity?.Name ?? "unknown");
            await LogAuditAsync("Logout", "Authentication", GetCurrentUserId(), "User", User?.Identity?.Name, "User logged out");
            HttpContext.Session.Clear();
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        // =============================================
        // BARANGAYS MANAGEMENT (super_admin only)
        // =============================================

        [HttpGet]
        [Authorize(Roles = "super_admin")]
        public async Task<IActionResult> BarangaysManagement(string q = "")
        {
            if (!IsSuperAdmin()) return RedirectToDashboard();

            var role = GetCurrentRole();
            var canModify = role == "super_admin";

            var barangays = await _context.Barangays
                .Where(b => b.IsActive)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            if (!string.IsNullOrWhiteSpace(q))
            {
                barangays = barangays
                    .Where(b => b.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                                (b.Code ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                                (b.Municipality ?? "").Contains(q, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var vm = new BarangaysManagementViewModel
            {
                SearchQuery = q,
                CanCreate = canModify,
                CanEdit = canModify,
                CanArchive = canModify,
                Barangays = barangays.Select(b => new BarangayItem
                {
                    Id = b.Id,
                    Name = b.Name,
                    Code = b.Code,
                    Municipality = b.Municipality,
                    Province = b.Province,
                    Region = b.Region,
                    ContactEmail = b.ContactEmail,
                    ContactPhone = b.ContactPhone,
                    Address = b.Address,
                    IsActive = b.IsActive,
                    CreatedAt = b.CreatedAt,
                    UpdatedAt = b.UpdatedAt
                }).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "super_admin")]
        public async Task<IActionResult> CreateBarangay(string name, string? code, string? municipality,
            string? province, string? region, string? contactEmail, string? contactPhone, string? address, string q = "")
        {
            if (!IsSuperAdmin()) return RedirectToDashboard();

            var barangay = new Barangay
            {
                Name = name,
                Code = code,
                Municipality = municipality,
                Province = province,
                Region = region,
                ContactEmail = contactEmail,
                ContactPhone = contactPhone,
                Address = address,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.Barangays.Add(barangay);
            await _context.SaveChangesAsync();
            await LogAuditAsync("Create", "BarangaysManagement", barangay.Id, "Barangay", name, $"Created barangay: {name}");

            return RedirectToAction("BarangaysManagement", new { q });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "super_admin")]
        public async Task<IActionResult> EditBarangay(int id, string name, string? code, string? municipality,
            string? province, string? region, string? contactEmail, string? contactPhone, string? address, string q = "")
        {
            if (!IsSuperAdmin()) return RedirectToDashboard();

            var barangay = await _context.Barangays.FindAsync(id);
            if (barangay != null)
            {
                barangay.Name = name;
                barangay.Code = code;
                barangay.Municipality = municipality;
                barangay.Province = province;
                barangay.Region = region;
                barangay.ContactEmail = contactEmail;
                barangay.ContactPhone = contactPhone;
                barangay.Address = address;
                barangay.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                await LogAuditAsync("Edit", "BarangaysManagement", barangay.Id, "Barangay", barangay.Name, $"Updated barangay: {barangay.Name}");
            }

            return RedirectToAction("BarangaysManagement", new { q });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "super_admin")]
        public async Task<IActionResult> ArchiveBarangay(int id, string q = "")
        {
            if (!IsSuperAdmin()) return RedirectToDashboard();

            var barangay = await _context.Barangays.FindAsync(id);
            if (barangay != null)
            {
                barangay.IsActive = false;
                barangay.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                await LogAuditAsync("Archive", "BarangaysManagement", barangay.Id, "Barangay", barangay.Name, $"Archived barangay: {barangay.Name}");
            }

            return RedirectToAction("BarangaysManagement", new { q });
        }

        // =============================================
        // KNOWLEDGE DISCUSSIONS (barangay module)
        // =============================================

        [HttpGet]
        public async Task<IActionResult> KnowledgeDiscussions(string q = "", string category = "All Categories")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            // MULTI-TENANT ISOLATION: Super_admin cannot access barangay internal data
            if (IsSuperAdmin()) return RedirectToAction("System", "Dashboard");

            var role = GetCurrentRole();
            var barangayId = GetCurrentBarangayId();
            
            if (!barangayId.HasValue)
            {
                TempData["Error"] = "No barangay assigned to your account.";
                return RedirectToAction("Barangay", "Dashboard");
            }

            var canModify = role == "barangay_admin" || role == "barangay_secretary" || role == "barangay_staff";

            // STRICT TENANT FILTERING: Only fetch discussions from user's barangay
            var discussions = await _context.KnowledgeDiscussions
                .Include(d => d.Author)
                .Where(d => d.IsActive)
                .Where(d => d.BarangayId == barangayId.Value)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            if (!string.IsNullOrWhiteSpace(q))
            {
                discussions = discussions
                    .Where(d => d.Title.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                                d.Content.Contains(q, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (category != "All Categories")
            {
                discussions = discussions.Where(d => d.Category == category).ToList();
            }

            var vm = new KnowledgeDiscussionsViewModel
            {
                SearchQuery = q,
                CategoryFilter = category,
                CanCreate = canModify,
                CanEdit = canModify,
                CanArchive = canModify,
                Discussions = discussions.Select(d => new DiscussionItem
                {
                    Id = d.Id,
                    Title = d.Title,
                    Content = d.Content,
                    Category = d.Category,
                    AuthorId = d.AuthorId,
                    AuthorName = d.Author?.FullName ?? "Unknown",
                    BarangayId = d.BarangayId,
                    LikesCount = d.LikesCount,
                    RepliesCount = d.RepliesCount,
                    IsActive = d.IsActive,
                    CreatedAt = d.CreatedAt,
                    UpdatedAt = d.UpdatedAt
                }).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [DenyViewOnly]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "super_admin,barangay_admin,barangay_staff")]
        public async Task<IActionResult> CreateKnowledgeDiscussion(string title, string content, string? category, string q = "", string categoryFilter = "All Categories")
        {
            var userId = GetCurrentUserId();
            var barangayId = GetCurrentBarangayId();
            if (!userId.HasValue) return RedirectToAction("Login");

            var discussion = new KnowledgeDiscussion
            {
                Title = title,
                Content = content,
                Category = category,
                AuthorId = userId.Value,
                BarangayId = barangayId,
                LikesCount = 0,
                RepliesCount = 0,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.KnowledgeDiscussions.Add(discussion);
            await _context.SaveChangesAsync();
            await LogAuditAsync("Create", "KnowledgeDiscussions", discussion.Id, "Discussion", title, $"Created discussion: {title}");

            return RedirectToAction("KnowledgeDiscussions", new { q, category = categoryFilter });
        }

        [HttpPost]
        [DenyViewOnly]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditKnowledgeDiscussion(int id, string title, string content, string? category, string q = "", string categoryFilter = "All Categories")
        {
            var discussion = await _context.KnowledgeDiscussions.FindAsync(id);
            if (discussion != null)
            {
                discussion.Title = title;
                discussion.Content = content;
                discussion.Category = category;
                discussion.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
            }

            return RedirectToAction("KnowledgeDiscussions", new { q, category = categoryFilter });
        }

        [HttpPost]
        [DenyViewOnly]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "super_admin,barangay_admin")]
        public async Task<IActionResult> ArchiveKnowledgeDiscussion(int id, string q = "", string categoryFilter = "All Categories")
        {
            var role = HttpContext.Session.GetString("Role") ?? "";
            if (role != "barangay_admin" && role != "super_admin")
                return RedirectToAction("KnowledgeDiscussions", new { q, category = categoryFilter });

            var discussion = await _context.KnowledgeDiscussions.FindAsync(id);
            if (discussion != null)
            {
                discussion.IsArchived = true;
                discussion.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("KnowledgeDiscussions", new { q, category = categoryFilter });
        }

        [HttpPost]
        [DenyViewOnly]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "super_admin,barangay_admin")]
        public async Task<IActionResult> RestoreKnowledgeDiscussion(int id, string q = "", string categoryFilter = "All Categories")
        {
            var role = HttpContext.Session.GetString("Role") ?? "";
            if (role != "barangay_admin" && role != "super_admin")
                return RedirectToAction("KnowledgeDiscussions", new { q, category = categoryFilter });

            var discussion = await _context.KnowledgeDiscussions.FindAsync(id);
            if (discussion != null)
            {
                discussion.IsArchived = false;
                discussion.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("KnowledgeDiscussions", new { q, category = categoryFilter, archiveStatus = "active" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LikeDiscussion(int id, string q = "", string categoryFilter = "All Categories")
        {
            var discussion = await _context.KnowledgeDiscussions.FindAsync(id);
            if (discussion != null)
            {
                discussion.LikesCount++;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("KnowledgeDiscussions", new { q, category = categoryFilter });
        }

        // =============================================
        // PAYMENT WORKFLOW (manual verification)
        // =============================================

        // GET: /Home/SelectPlan — Barangay Admin picks a subscription plan
        // UPDATED: Also allow access for pending registrations (not logged in yet)
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> SelectPlan(int? planId = null)
        {
            // Check for pending registration (user not created yet)
            var hasPendingReg = !string.IsNullOrWhiteSpace(GetValidPendingRegistrationJson());
            
            // If not logged in AND no pending registration, redirect to login
            if (!IsLoggedIn() && !hasPendingReg)
            {
                return RedirectToAction(nameof(Login));
            }
            
            // If logged in, check role (existing behavior)
            if (IsLoggedIn() && GetCurrentRole() != "barangay_admin")
            {
                return RedirectToDashboard();
            }

            var barangayId = GetCurrentBarangayId();

            // Check if barangay already has an active or pending subscription (only for logged in users)
            var existing = false;
            if (barangayId != null)
            {
                existing = await _context.BarangaySubscriptions
                    .AnyAsync(s => s.IsActive && s.BarangayId == barangayId
                        && (s.Status == "Active" || s.Status == "Pending")
                        && s.EndDate >= DateTime.Today);
            }

            // Get all active plans, then dedupe by name in memory
            var allPlans = await _context.SubscriptionPlans
                .Where(p => p.IsActive)
                .OrderBy(p => p.Price)
                .ToListAsync();

            // Filter out duplicate plans by name (keep the lowest price per name)
            var plans = allPlans
                .GroupBy(p => p.Name)
                .Select(g => g.First())
                .OrderBy(p => p.Price)
                .Select(p => new SelectPlanViewModel.AvailablePlan
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description ?? "",
                    Price = p.Price,
                    DurationMonths = p.DurationMonths,
                    UserLimit = p.UserLimit,
                    Features = p.Features
                })
                .ToList();

            var vm = new SelectPlanViewModel
            {
                Plans = plans,
                HasActiveSubscription = existing,
                HasBarangay = barangayId != null,
                SelectedPlanId = planId,
                SuccessMessage = TempData["Success"] as string,
                ErrorMessage = TempData["Error"] as string
            };

            return View(vm);
        }

        // POST: /Home/SubscribeToPlan — Create Pending subscription + Unpaid invoice
        // UPDATED: Also handles pending registrations - creates user if needed
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> SubscribeToPlan(int planId)
        {
            int? barangayId = null;
            
            // Check for pending registration (new user flow)
            var pendingRegJson = GetValidPendingRegistrationJson();
            if (!string.IsNullOrEmpty(pendingRegJson))
            {
                // Parse pending registration data
                var pendingReg = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(pendingRegJson);
                if (pendingReg == null)
                {
                    TempData["Error"] = "Registration data expired. Please register again.";
                    return RedirectToAction(nameof(Register));
                }

                var email = pendingReg["Email"]?.ToString() ?? "";
                var password = pendingReg["Password"]?.ToString() ?? "";
                var firstName = pendingReg["FirstName"]?.ToString() ?? "";
                var lastName = pendingReg["LastName"]?.ToString() ?? "";
                var phoneNumber = pendingReg["PhoneNumber"]?.ToString() ?? "";
                var barangayName = pendingReg["BarangayName"]?.ToString() ?? "";
                var municipality = pendingReg["Municipality"]?.ToString() ?? "";
                var province = pendingReg["Province"]?.ToString() ?? "";
                var region = pendingReg["Region"]?.ToString() ?? "";
                var address = pendingReg["Address"]?.ToString() ?? "";

                // Double-check user doesn't exist (race condition protection)
                var existingUser = await _userManager.FindByEmailAsync(email);
                if (existingUser != null)
                {
                    HttpContext.Session.Remove(PendingRegistrationKey);
                    HttpContext.Session.Remove(PendingRegistrationCreatedAtKey);
                    TempData["Error"] = "An account with this email already exists. Please login.";
                    return RedirectToAction(nameof(Login));
                }

                // Create the Barangay record
                var barangay = new Barangay
                {
                    Name = barangayName,
                    Municipality = municipality,
                    Province = province,
                    Region = region,
                    Address = address,
                    ContactEmail = email,
                    ContactPhone = phoneNumber,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };
                _context.Barangays.Add(barangay);
                await _context.SaveChangesAsync();

                // Create the Identity User
                var user = new IdentityUser { UserName = email, Email = email, PhoneNumber = phoneNumber };
                var result = await _userManager.CreateAsync(user, password);

                if (!result.Succeeded)
                {
                    // Cleanup barangay if user creation failed
                    _context.Barangays.Remove(barangay);
                    await _context.SaveChangesAsync();
                    
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    TempData["Error"] = $"Failed to create account: {errors}";
                    return RedirectToAction(nameof(Register));
                }

                // Assign role 'barangay_admin'
                await _userManager.AddToRoleAsync(user, "barangay_admin");

                // Create BusinessUser (User entity)
                var businessUser = new Models.Entities.User
                {
                    Email = email,
                    FullName = $"{firstName} {lastName}".Trim(),
                    PhoneNumber = phoneNumber,
                    PasswordHash = "IDENTITY_MANAGED",
                    Role = "barangay_admin",
                    BarangayId = barangay.Id,
                    BarangayName = barangay.Name,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };
                _context.BusinessUsers.Add(businessUser);
                await _context.SaveChangesAsync();

                _logger.LogInformation("New Barangay Registered during subscription: {Barangay} by {Email}", barangay.Name, email);

                // Sign in the user
                await _signInManager.SignInAsync(user, isPersistent: false);

                // Set session values
                HttpContext.Session.SetString("UserId", businessUser.Id.ToString());
                HttpContext.Session.SetString("UserName", email);
                HttpContext.Session.SetString("FullName", businessUser.FullName);
                HttpContext.Session.SetString("Role", "barangay_admin");
                HttpContext.Session.SetString("RoleLabel", "Barangay Admin");
                HttpContext.Session.SetString("BarangayId", barangay.Id.ToString());
                HttpContext.Session.SetString("Barangay", barangay.Name);

                // Clear pending registration
                HttpContext.Session.Remove(PendingRegistrationKey);
                HttpContext.Session.Remove(PendingRegistrationCreatedAtKey);

                barangayId = barangay.Id;

                await LogAuditAsync("Register", "Authentication", businessUser.Id, "User", email, $"New barangay registration: {barangay.Name}");
            }
            else
            {
                // Existing user flow - must be logged in
                if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
                if (GetCurrentRole() != "barangay_admin") return RedirectToDashboard();
                
                barangayId = GetCurrentBarangayId();
            }
            
            if (barangayId == null)
            {
                TempData["Error"] = "No barangay associated with your account.";
                return RedirectToAction(nameof(SelectPlan));
            }

            var plan = await _context.SubscriptionPlans.FindAsync(planId);
            if (plan == null || !plan.IsActive)
            {
                TempData["Error"] = "Invalid or inactive plan.";
                return RedirectToAction(nameof(SelectPlan));
            }

            // Prevent duplicate pending subscriptions
            var hasPending = await _context.BarangaySubscriptions
                .AnyAsync(s => s.IsActive && s.BarangayId == barangayId && s.Status == "Pending");
            if (hasPending)
            {
                TempData["Error"] = "You already have a pending subscription. Please complete payment first.";
                return RedirectToAction(nameof(MySubscription));
            }

            // Enforce user limit: check current users vs plan limit
            var currentUserCount = await _context.BusinessUsers
                .CountAsync(u => u.IsActive && u.BarangayId == barangayId);
            if (currentUserCount > plan.UserLimit)
            {
                TempData["Error"] = $"Your barangay has {currentUserCount} users but the {plan.Name} plan only allows {plan.UserLimit}. Please choose a higher plan or remove some users.";
                return RedirectToAction(nameof(SelectPlan));
            }

            // Create subscription with Pending status
            var subscription = new BarangaySubscription
            {
                BarangayId = barangayId.Value,
                PlanId = plan.Id,
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddMonths(plan.DurationMonths),
                Status = "Pending",
                IsActive = true,
                CreatedAt = DateTime.Now
            };
            _context.BarangaySubscriptions.Add(subscription);
            await _context.SaveChangesAsync();

            // Create invoice
            var invoiceNumber = $"INV-{DateTime.Now:yyyyMMdd}-{subscription.Id:D5}";
            var invoice = new Invoice
            {
                InvoiceNumber = invoiceNumber,
                SubscriptionId = subscription.Id,
                BarangayId = barangayId.Value,
                Amount = plan.Price,
                DueDate = DateTime.Today.AddDays(7),
                Status = "Unpaid",
                IssuedAt = DateTime.Now,
                Notes = $"Subscription to {plan.Name} ({plan.DurationMonths} month{(plan.DurationMonths > 1 ? "s" : "")})",
                CreatedAt = DateTime.Now
            };
            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            // Create initial pending payment record (no proof yet)
            var payment = new SubscriptionPayment
            {
                SubscriptionId = subscription.Id,
                InvoiceId = invoice.Id,
                Amount = plan.Price,
                PaymentDate = DateTime.Today,
                PaymentMethod = "",
                Status = "Pending",
                IsActive = true,
                CreatedAt = DateTime.Now
            };
            _context.SubscriptionPayments.Add(payment);
            await _context.SaveChangesAsync();

            var barangayNameForLog = HttpContext.Session.GetString("Barangay") ?? "Barangay";
            await LogAuditAsync("Create", "SubscriptionPayments", subscription.Id, "Subscription",
                $"{barangayNameForLog} - {plan.Name}", $"Subscribed to {plan.Name} (₱{plan.Price:N0}/mo). Invoice {invoiceNumber} generated.");

            TempData["Success"] = $"Subscription created! Invoice {invoiceNumber} for ₱{plan.Price:N0} has been generated.";

            // Redirect to the new "Checkout" style payment page
            return RedirectToAction(nameof(SubmitPayment), new { invoiceId = invoice.Id });
        }

        // POST: /Home/CancelPendingSubscription — Barangay Admin cancels their pending subscription to create a new one
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelPendingSubscription(int subscriptionId)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (GetCurrentRole() != "barangay_admin") return RedirectToDashboard();

            var barangayId = GetCurrentBarangayId();
            
            var subscription = await _context.BarangaySubscriptions
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s => s.Id == subscriptionId && s.IsActive && s.BarangayId == barangayId && s.Status == "Pending");

            if (subscription == null)
            {
                TempData["Error"] = "Subscription not found or cannot be cancelled.";
                return RedirectToAction(nameof(MySubscription));
            }

            // Check if there are any blocking payments (Paid, Approved, or PendingVerification)
            var hasBlockingPayment = await _context.SubscriptionPayments
                .AnyAsync(p => p.SubscriptionId == subscriptionId && p.IsActive && 
                    (p.Status == "Paid" || p.Status == "Approved" || p.Status == "PendingVerification"));

            if (hasBlockingPayment)
            {
                TempData["Error"] = "Cannot cancel subscription with pending or approved payments.";
                return RedirectToAction(nameof(MySubscription));
            }

            // Cancel the subscription
            subscription.Status = "Cancelled";
            subscription.IsActive = false;
            subscription.UpdatedAt = DateTime.Now;

            // Cancel related invoices
            var relatedInvoices = await _context.Invoices
                .Where(i => i.SubscriptionId == subscriptionId && i.IsActive)
                .ToListAsync();

            foreach (var invoice in relatedInvoices)
            {
                invoice.Status = "Void";
                invoice.IsActive = false;
                invoice.UpdatedAt = DateTime.Now;
            }

            // Soft-delete related rejected payments
            var rejectedPayments = await _context.SubscriptionPayments
                .Where(p => p.SubscriptionId == subscriptionId && p.IsActive && p.Status == "Rejected")
                .ToListAsync();

            foreach (var payment in rejectedPayments)
            {
                payment.IsActive = false;
                payment.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            var planName = subscription.Plan?.Name ?? "Unknown Plan";
            await LogAuditAsync("Cancel", "BarangaySubscriptions", subscription.Id, "Subscription",
                planName, $"Cancelled pending subscription to {planName}");

            TempData["Success"] = "Subscription cancelled. You can now subscribe to a new plan.";
            return RedirectToAction(nameof(SelectPlan));
        }

        // POST: /Home/InitiateOnlinePayment — Creates a PayMongo Checkout Session and redirects
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InitiateOnlinePayment(int invoiceId, string paymentMethod = "")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (GetCurrentRole() != "barangay_admin") return RedirectToDashboard();

            var barangayId = GetCurrentBarangayId();
            var invoice = await _context.Invoices
                .Include(i => i.Subscription)
                .ThenInclude(s => s!.Plan)
                .FirstOrDefaultAsync(i => i.Id == invoiceId && i.IsActive && i.BarangayId == barangayId);

            if (invoice == null || invoice.Status == "Paid")
            {
                TempData["Error"] = "Invalid invoice or already paid.";
                return RedirectToAction(nameof(MySubscription));
            }

            var scheme = Request.Scheme;
            var host = Request.Host.Value;
            var successUrl = $"{scheme}://{host}{Url.Action("PaymentSuccess", "Home", new { invoiceId = invoice.Id })}";
            var cancelUrl = $"{scheme}://{host}{Url.Action("PaymentCancel", "Home", new { invoiceId = invoice.Id })}";

            // Normalize payment method (gcash or card)
            var normalizedPaymentMethod = paymentMethod?.ToLowerInvariant() switch
            {
                "gcash" => "gcash",
                "card" => "card",
                _ => null // Will use all payment methods
            };

            var checkoutUrl = await _payMongoService.CreateCheckoutSessionAsync(
                invoice.Amount,
                $"JAS-MINE: {invoice.Subscription?.Plan?.Name} Subscription",
                successUrl,
                cancelUrl,
                invoice.InvoiceNumber,
                normalizedPaymentMethod
            );

            if (!string.IsNullOrEmpty(checkoutUrl))
            {
                return Redirect(checkoutUrl);
            }

            TempData["Error"] = "Could not initialize automated payment. Please try manual upload.";
            return RedirectToAction(nameof(SubmitPayment), new { invoiceId = invoice.Id });
        }

        [HttpGet]
        public async Task<IActionResult> PaymentSuccess(int invoiceId)
        {
            // Fetch the invoice with subscription and plan details
            var invoice = await _context.Invoices
                .Include(i => i.Subscription)
                    .ThenInclude(s => s!.Plan)
                .Include(i => i.Subscription)
                    .ThenInclude(s => s!.Barangay)
                .FirstOrDefaultAsync(i => i.Id == invoiceId && i.IsActive);

            if (invoice?.Subscription != null)
            {
                ViewBag.CompanyName = invoice.Subscription.Barangay?.Name ?? "Your Barangay";
                ViewBag.PlanName = invoice.Subscription.Plan?.Name ?? "Subscription";
                ViewBag.Email = invoice.Subscription.Barangay?.ContactEmail ?? "";
                ViewBag.Status = invoice.Subscription.Status == "active" ? "Active" : invoice.Subscription.Status;
            }
            else
            {
                ViewBag.CompanyName = "Your Barangay";
                ViewBag.PlanName = "Subscription";
                ViewBag.Email = "";
                ViewBag.Status = "Active";
            }

            return View();
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult StatusCodePage(int code)
        {
            Response.StatusCode = code;
            ViewBag.StatusCode = code;
            return View();
        }

        [HttpGet]
        public IActionResult PaymentCancel(int invoiceId)
        {
            TempData["Error"] = "Payment was cancelled. You can try paying again from your subscription page.";
            return RedirectToAction(nameof(MySubscription));
        }

        // POST: /Home/UploadPaymentProof — Barangay Admin uploads proof-of-payment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPaymentProof(int invoiceId, IFormFile proofFile, string paymentMethod, string? referenceNumber)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (GetCurrentRole() != "barangay_admin") return RedirectToDashboard();

            var barangayId = GetCurrentBarangayId();
            var invoice = await _context.Invoices
                .Include(i => i.Subscription)
                .FirstOrDefaultAsync(i => i.Id == invoiceId && i.IsActive && i.BarangayId == barangayId);

            if (invoice == null)
            {
                TempData["Error"] = "Invoice not found.";
                return RedirectToAction(nameof(MySubscription));
            }

            if (invoice.Status == "Paid")
            {
                TempData["Error"] = "This invoice has already been paid.";
                return RedirectToAction(nameof(MySubscription));
            }

            // Prevent duplicate uploads — only allow if no PendingVerification payment exists for this invoice
            var hasPendingProof = await _context.SubscriptionPayments
                .AnyAsync(p => p.InvoiceId == invoiceId && p.IsActive && p.Status == "PendingVerification");
            if (hasPendingProof)
            {
                TempData["Error"] = "A payment proof is already pending verification for this invoice.";
                return RedirectToAction(nameof(MySubscription));
            }

            if (proofFile == null || proofFile.Length == 0)
            {
                TempData["Error"] = "Please upload a proof of payment file.";
                return RedirectToAction(nameof(MySubscription));
            }

            // Validate file type and size (max 5 MB, images and PDFs only)
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
            var ext = Path.GetExtension(proofFile.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
            {
                TempData["Error"] = "Only JPG, PNG, and PDF files are allowed.";
                return RedirectToAction(nameof(MySubscription));
            }
            if (proofFile.Length > 5 * 1024 * 1024)
            {
                TempData["Error"] = "File size must be under 5 MB.";
                return RedirectToAction(nameof(MySubscription));
            }

            // Save file to wwwroot/uploads/payments
            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "payments");
            Directory.CreateDirectory(uploadsDir);
            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadsDir, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await proofFile.CopyToAsync(stream);
            }

            var proofUrl = $"/uploads/payments/{fileName}";

            // Create payment record
            var payment = new SubscriptionPayment
            {
                SubscriptionId = invoice.SubscriptionId,
                InvoiceId = invoice.Id,
                Amount = invoice.Amount,
                PaymentDate = DateTime.Today,
                PaymentMethod = (paymentMethod ?? "").Trim(),
                ReferenceNumber = (referenceNumber ?? "").Trim(),
                ProofOfPaymentUrl = proofUrl,
                Status = "PendingVerification",
                IsActive = true,
                CreatedAt = DateTime.Now
            };
            _context.SubscriptionPayments.Add(payment);
            await _context.SaveChangesAsync();

            await LogAuditAsync("Upload", "SubscriptionPayments", payment.Id, "Payment",
                $"Invoice {invoice.InvoiceNumber}", $"Uploaded proof of payment for ₱{invoice.Amount:N0}");

            TempData["Success"] = "Proof of payment uploaded. It will be verified by the system administrator.";
            return RedirectToAction(nameof(MySubscription));
        }

        // GET: /Home/PendingPayments — Super Admin reviews pending payment proofs
        [HttpGet]
        [Authorize(Roles = "super_admin")]
        public async Task<IActionResult> PendingPayments()
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsSuperAdmin()) return RedirectToDashboard();

            var pendingPayments = await _context.SubscriptionPayments
                .Where(p => p.IsActive && (p.Status == "Pending" || p.Status == "PendingVerification"))
                .Include(p => p.Invoice)
                .Include(p => p.Subscription)
                    .ThenInclude(s => s!.Barangay)
                .Include(p => p.Subscription)
                    .ThenInclude(s => s!.Plan)
                .OrderBy(p => p.CreatedAt)
                .Select(p => new PendingPaymentsViewModel.PendingPaymentRow
                {
                    PaymentId = p.Id,
                    BarangayName = p.Subscription != null && p.Subscription.Barangay != null ? p.Subscription.Barangay.Name : "Unknown",
                    PlanName = p.Subscription != null && p.Subscription.Plan != null ? p.Subscription.Plan.Name : "Unknown",
                    InvoiceNumber = p.Invoice != null ? p.Invoice.InvoiceNumber : "",
                    Amount = p.Amount,
                    PaymentDate = p.PaymentDate.ToString("yyyy-MM-dd"),
                    PaymentMethod = p.PaymentMethod ?? "",
                    ProofUrl = p.ProofOfPaymentUrl,
                    ReferenceNumber = p.ReferenceNumber
                })
                .ToListAsync();

            var vm = new PendingPaymentsViewModel
            {
                Payments = pendingPayments,
                SuccessMessage = TempData["Success"] as string,
                ErrorMessage = TempData["Error"] as string
            };

            return View(vm);
        }

        // POST: /Home/ApprovePayment — Super Admin approves a payment
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "super_admin")]
        public async Task<IActionResult> ApprovePayment(int paymentId, string q = "")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsSuperAdmin()) return RedirectToDashboard();


            // Allow approval of both 'Pending' and 'PendingVerification' payments
            var payment = await _context.SubscriptionPayments
                .Include(p => p.Invoice)
                .Include(p => p.Subscription)
                    .ThenInclude(s => s!.Plan)
                .FirstOrDefaultAsync(p => p.Id == paymentId && p.IsActive && (p.Status == "PendingVerification" || p.Status == "Pending"));

            if (payment == null)
            {
                TempData["Error"] = "Payment not found or already processed.";
                return RedirectToAction(nameof(SubscriptionPayments), new { q });
            }

            // Approve the payment
            payment.Status = "Approved";
            payment.ProcessedById = GetCurrentUserId();
            payment.ProcessedAt = DateTime.Now;
            payment.UpdatedAt = DateTime.Now;

            // Mark invoice as paid
            if (payment.Invoice != null)
            {
                payment.Invoice.Status = "Paid";
                payment.Invoice.PaidAt = DateTime.Now;
                payment.Invoice.UpdatedAt = DateTime.Now;
            }

            // Activate subscription
            if (payment.Subscription != null)
            {
                payment.Subscription.Status = "Active";
                payment.Subscription.StartDate = DateTime.Today;
                var duration = payment.Subscription.Plan?.DurationMonths ?? 12;
                payment.Subscription.EndDate = DateTime.Today.AddMonths(duration);
                payment.Subscription.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            var label = payment.Invoice?.InvoiceNumber ?? $"Payment #{payment.Id}";
            await LogAuditAsync("Approve", "SubscriptionPayments", payment.Id, "Payment", label,
                $"Approved payment of ₱{payment.Amount:N0} for {label}");

            // PART 1: Check if barangay has at least one admin
            var barangayId = payment.Subscription?.BarangayId;
            if (barangayId.HasValue)
            {
                var hasAdmin = await CheckBarangayAdminExistsAsync(barangayId.Value);
                if (!hasAdmin)
                {
                    // No admin exists - redirect to create one
                    TempData["Info"] = $"Payment approved! Please create a Barangay Administrator for this subscription.";
                    return RedirectToAction(nameof(CreateBarangayAdmin), new
                    {
                        barangayId = barangayId.Value,
                        subscriptionId = payment.Subscription!.Id,
                        paymentId = payment.Id
                    });
                }
            }

            TempData["Success"] = $"Payment {label} approved. Subscription is now active.";
            return RedirectToAction(nameof(SubscriptionPayments), new { q, status = "PendingVerification" });
        }

        // GET: /Home/CreateBarangayAdmin — Super Admin creates a barangay admin after payment approval
        [HttpGet]
        [Authorize(Roles = "super_admin")]
        public async Task<IActionResult> CreateBarangayAdmin(int barangayId, int subscriptionId, int paymentId)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsSuperAdmin()) return RedirectToDashboard();

            // Verify barangay exists
            var barangay = await _context.Barangays.FindAsync(barangayId);
            if (barangay == null)
            {
                TempData["Error"] = "Barangay not found.";
                return RedirectToAction(nameof(PendingPayments));
            }

            // Check if admin already exists (prevent duplicate creation)
            var hasAdmin = await CheckBarangayAdminExistsAsync(barangayId);
            if (hasAdmin)
            {
                TempData["Info"] = "This barangay already has an administrator.";
                return RedirectToAction(nameof(PendingPayments));
            }

            var model = new CreateBarangayAdminViewModel
            {
                BarangayId = barangayId,
                BarangayName = barangay.Name,
                SubscriptionId = subscriptionId,
                PaymentId = paymentId
            };

            ViewData["Title"] = "Create Barangay Administrator";
            return View(model);
        }

        // POST: /Home/CreateBarangayAdmin — Create the barangay admin user
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "super_admin")]
        public async Task<IActionResult> CreateBarangayAdmin(CreateBarangayAdminViewModel model)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsSuperAdmin()) return RedirectToDashboard();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Check if admin already exists
            var hasAdmin = await CheckBarangayAdminExistsAsync(model.BarangayId);
            if (hasAdmin)
            {
                TempData["Error"] = "This barangay already has an administrator.";
                return RedirectToAction(nameof(PendingPayments));
            }

            // Check if email already exists in Identity
            var existingIdentityUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingIdentityUser != null)
            {
                ModelState.AddModelError("Email", "This email is already registered.");
                return View(model);
            }

            // Check if email exists in BusinessUsers
            var existingBusinessUser = await _context.BusinessUsers
                .AnyAsync(u => u.Email == model.Email);
            if (existingBusinessUser)
            {
                ModelState.AddModelError("Email", "This email is already registered.");
                return View(model);
            }

            try
            {
                // 1. Create ASP.NET Identity user
                var identityUser = new IdentityUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(identityUser, model.Password);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    ModelState.AddModelError("", $"Failed to create user: {errors}");
                    return View(model);
                }

                // 2. Assign barangay_admin role
                await _userManager.AddToRoleAsync(identityUser, "barangay_admin");

                // 3. Create BusinessUser record
                var barangay = await _context.Barangays.FindAsync(model.BarangayId);
                var businessUser = new Models.Entities.User
                {
                    FullName = model.FullName,
                    Email = model.Email,
                    PasswordHash = "IDENTITY_MANAGED",
                    Role = "barangay_admin",
                    BarangayId = model.BarangayId,
                    BarangayName = barangay?.Name ?? "",
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    CreatedBy = GetCurrentUserId()
                };

                _context.BusinessUsers.Add(businessUser);
                await _context.SaveChangesAsync();

                // 4. Log the action
                await LogAuditAsync("Create", "Users", businessUser.Id, "BarangayAdmin",
                    $"{model.FullName} ({model.Email})",
                    $"Barangay admin created after payment approval for {barangay?.Name ?? "barangay"}");

                TempData["Success"] = $"Barangay Administrator '{model.FullName}' created successfully for {barangay?.Name}. They can now log in.";
                return RedirectToAction(nameof(PendingPayments));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"An error occurred: {ex.Message}");
                return View(model);
            }
        }

        // POST: /Home/RejectPayment — Super Admin rejects a payment with reason
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "super_admin")]
        public async Task<IActionResult> RejectPayment(int paymentId, string rejectionReason = "", string q = "")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsSuperAdmin()) return RedirectToDashboard();

            var payment = await _context.SubscriptionPayments
                .Include(p => p.Invoice)
                .FirstOrDefaultAsync(p => p.Id == paymentId && p.IsActive && p.Status == "PendingVerification");

            if (payment == null)
            {
                TempData["Error"] = "Payment not found or already processed.";
                return RedirectToAction(nameof(SubscriptionPayments), new { q });
            }

            payment.Status = "Rejected";
            payment.RejectionReason = (rejectionReason ?? "").Trim();
            payment.ProcessedById = GetCurrentUserId();
            payment.ProcessedAt = DateTime.Now;
            payment.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            var label = payment.Invoice?.InvoiceNumber ?? $"Payment #{payment.Id}";
            await LogAuditAsync("Reject", "SubscriptionPayments", payment.Id, "Payment", label,
                $"Rejected payment of ₱{payment.Amount:N0}. Reason: {payment.RejectionReason}");

            TempData["Success"] = $"Payment {label} rejected.";
            return RedirectToAction(nameof(SubscriptionPayments), new { q, status = "PendingVerification" });
        }

        // GET: /Home/SubmitPayment?invoiceId=... — Redesigned standalone manual payment page
        public async Task<IActionResult> SubmitPayment(int invoiceId)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (GetCurrentRole() != "barangay_admin") return RedirectToDashboard();

            var barangayId = GetCurrentBarangayId();
            var invoice = await _context.Invoices
                .Include(i => i.Subscription)
                .ThenInclude(s => s!.Plan)
                .FirstOrDefaultAsync(i => i.Id == invoiceId && i.IsActive && i.BarangayId == barangayId);

            if (invoice == null)
            {
                TempData["Error"] = "Invoice not found.";
                return RedirectToAction(nameof(MySubscription));
            }

            if (invoice.Status == "Paid")
            {
                TempData["Success"] = "This invoice has already been paid.";
                return RedirectToAction(nameof(MySubscription));
            }

            // Check if there's already a pending payment
            var hasPending = await _context.SubscriptionPayments
                .AnyAsync(p => p.InvoiceId == invoiceId && p.IsActive && p.Status == "PendingVerification");

            ViewBag.HasPending = hasPending;
            ViewBag.BarangayName = HttpContext.Session.GetString("Barangay") ?? "Your Barangay";

            return View(invoice);
        }

        private static string GetRoleLabel(string role)
        {
            return role switch
            {
                "super_admin" => "Super Admin",
                "barangay_admin" => "Barangay Admin",
                "barangay_secretary" => "Barangay Secretary",
                "barangay_staff" => "Barangay Staff",
                "council_member" => "Council Member",
                _ => "User"
            };
        }
    }
}
