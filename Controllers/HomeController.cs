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
using System.Threading.Tasks;
using System.Security.Claims;

namespace JAS_MINE_IT15.Controllers
{
    public class HomeController : Controller
    {
        // ✅ Identity services (needed for DB login)
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;

        public HomeController(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            ApplicationDbContext context)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _context = context;
        }

        #region Helper Methods

        private bool IsLoggedIn() =>
            !string.IsNullOrEmpty(HttpContext.Session.GetString("UserName"));

        private bool IsAdminRole()
        {
            var role = HttpContext.Session.GetString("Role");
            return role == "super_admin" || role == "barangay_admin";
        }

        /// <summary>
        /// Gets the current user's role from session.
        /// </summary>
        private string GetCurrentRole()
        {
            return HttpContext.Session.GetString("Role") ?? "";
        }

        /// <summary>
        /// Gets the current user's BarangayId from session.
        /// </summary>
        private int? GetCurrentBarangayId()
        {
            var barangayIdStr = HttpContext.Session.GetString("BarangayId");
            if (int.TryParse(barangayIdStr, out var id))
                return id;
            return null;
        }

        /// <summary>
        /// Checks if current user is super_admin (system-wide access).
        /// </summary>
        private bool IsSuperAdmin()
        {
            return GetCurrentRole() == "super_admin";
        }

        /// <summary>
        /// Gets the current user's ID from session.
        /// </summary>
        private int? GetCurrentUserId()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (int.TryParse(userIdStr, out var id))
                return id;
            return null;
        }

        /// <summary>
        /// Checks if current user has view-only access (council_member).
        /// </summary>
        private bool IsViewOnly()
        {
            return GetCurrentRole() == "council_member";
        }

        /// <summary>
        /// Checks if current user can create/edit/delete (not council_member).
        /// </summary>
        private bool CanModify()
        {
            var role = GetCurrentRole();
            return role == "barangay_admin" || role == "barangay_secretary" || role == "barangay_staff";
        }

        /// <summary>
        /// Checks if current user is a barangay role (not super_admin).
        /// Super_admin only monitors system, does not access barangay modules.
        /// </summary>
        private bool IsBarangayRole()
        {
            var role = GetCurrentRole();
            return role == "barangay_admin" || role == "barangay_secretary" || role == "barangay_staff" || role == "council_member";
        }

        /// <summary>
        /// Redirects to the appropriate dashboard based on current user's role.
        /// </summary>
        private IActionResult RedirectToDashboard()
        {
            var role = GetCurrentRole();
            if (role == "super_admin")
                return RedirectToAction("System", "Dashboard");
            return RedirectToAction("Barangay", "Dashboard");
        }

        #endregion

        private static string ComputeStatus(string endDate)
        {
            if (DateTime.TryParse(endDate, out var end))
                return end.Date >= DateTime.Today ? "Active" : "Expired";
            return "Expired";
        }

        // GET: Home Index
        [HttpGet]
        public IActionResult Index()
        {
            // If already logged in, go dashboard
            if (IsLoggedIn())
                return RedirectToDashboard();

            // If not logged in, show landing page
            return View("LandingPage");
        }

        // GET: /Home/LandingPage
        [HttpGet]
        public IActionResult LandingPage()
        {
            // Public page (no login required)
            return View();
        }

        // GET: /Home/BarangaySubscriptions
        [HttpGet]
        public IActionResult BarangaySubscriptions(string q = "", string status = "all")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToDashboard();

            q = (q ?? "").Trim();
            status = (status ?? "all").Trim();

            // TODO: Fetch subscriptions from database
            // var allSubscriptions = _context.Subscriptions.ToList();
            var allSubscriptions = new List<SubscriptionItem>();

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

            // TODO: Fetch barangays and plans from database
            // var barangays = _context.Barangays.Select(b => b.Name).ToList();
            // var plans = _context.Plans.Select(p => p.Name).ToList();
            var barangays = new List<string>();
            var plans = new List<string>();

            var vm = new BarangaySubscriptionsViewModel
            {
                SearchQuery = q,
                StatusFilter = status,
                Subscriptions = list,

                TotalCount = allSubscriptions.Count,
                ActiveCount = allSubscriptions.Count(x => x.Status == "Active"),
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
        public IActionResult CreateSubscription(string barangayName, string planName, string startDate, string endDate, string q = "", string status = "all")
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

            // TODO: Add subscription to database
            TempData["Success"] = $"{planName} assigned to {barangayName}.";
            return RedirectToAction(nameof(BarangaySubscriptions), new { q, status });
        }

        // POST: Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
        public IActionResult EditSubscription(string id, string barangayName, string planName, string startDate, string endDate, string q = "", string status = "all")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToDashboard();

            SubscriptionItem? item = null;

            if (item == null)
            {
                TempData["Error"] = "Subscription not found.";
                return RedirectToAction(nameof(BarangaySubscriptions), new { q, status });
            }

            item.BarangayName = (barangayName ?? item.BarangayName).Trim();
            item.PlanName = (planName ?? item.PlanName).Trim();
            item.StartDate = (startDate ?? item.StartDate).Trim();
            item.EndDate = (endDate ?? item.EndDate).Trim();

            if (item.Status != "Cancelled")
                item.Status = ComputeStatus(item.EndDate);

            TempData["Success"] = $"Subscription for {item.BarangayName} updated.";
            return RedirectToAction(nameof(BarangaySubscriptions), new { q, status });
        }

        // POST: Cancel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CancelSubscription(string id, string q = "", string status = "all")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToDashboard();

            TempData["Success"] = "Subscription cancelled.";
            return RedirectToAction(nameof(BarangaySubscriptions), new { q, status });
        }

        // GET: /Home/MySubscription
        [HttpGet]
        public IActionResult MySubscription()
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            var barangay = HttpContext.Session.GetString("Barangay") ?? "Your Barangay";

            var vm = new MySubscriptionViewModel
            {
                BarangayName = barangay,
                Subscription = null,
                Payments = new List<MySubscriptionViewModel.PaymentRow>()
            };

            return View(vm);
        }

        // GET: /Home/SubscriptionPayments
        [HttpGet]
        public IActionResult SubscriptionPayments(string q = "")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToDashboard();

            q = (q ?? "").Trim();

            var allPayments = new List<PaymentItem>();

            var list = allPayments.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var qq = q.ToLower();
                list = list.Where(p =>
                    (p.BarangayName ?? "").ToLower().Contains(qq) ||
                    (p.PlanName ?? "").ToLower().Contains(qq)
                );
            }

            var filtered = list.ToList();

            var totalPaid = allPayments.Where(p => p.Status == "Paid").Sum(p => p.Amount);

            var barangays = new List<string>();
            var plans = new List<string>();
            var methods = new List<string> { "Cash", "Bank Transfer", "GCash", "Maya", "Check" };

            var vm = new SubscriptionPaymentsViewModel
            {
                SearchQuery = q,
                Payments = filtered,

                TotalPayments = allPayments.Count,
                TotalCollected = totalPaid,
                PendingCount = allPayments.Count(p => p.Status == "Pending"),
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
        public IActionResult CreatePayment(string barangayName, string planName, decimal amount, string paymentDate, string paymentMethod, string status, string q = "")
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

            TempData["Success"] = $"Payment of ₱{amount:N0} recorded.";
            return RedirectToAction(nameof(SubscriptionPayments), new { q });
        }

        // POST: Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
        public IActionResult EditPayment(string id, string barangayName, string planName, decimal amount, string paymentDate, string paymentMethod, string status, string q = "")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToDashboard();

            PaymentItem? p = null;

            if (p == null)
            {
                TempData["Error"] = "Payment record not found.";
                return RedirectToAction(nameof(SubscriptionPayments), new { q });
            }

            p.BarangayName = (barangayName ?? p.BarangayName).Trim();
            p.PlanName = (planName ?? p.PlanName).Trim();
            p.Amount = amount <= 0 ? p.Amount : amount;
            p.PaymentDate = string.IsNullOrWhiteSpace(paymentDate) ? p.PaymentDate : paymentDate.Trim();
            p.PaymentMethod = string.IsNullOrWhiteSpace(paymentMethod) ? p.PaymentMethod : paymentMethod.Trim();
            p.Status = string.IsNullOrWhiteSpace(status) ? p.Status : status.Trim();

            TempData["Success"] = "Payment record has been updated.";
            return RedirectToAction(nameof(SubscriptionPayments), new { q });
        }

        // POST: Archive
        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
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
        public IActionResult SubscriptionPlans(string q = "")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToDashboard();

            q = (q ?? "").Trim();

            var allPlans = new List<PlanItem>();

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
        public IActionResult CreatePlan(string name, decimal price, int durationMonths, string description, bool isActive, string q = "")
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

            TempData["Success"] = $"\"{name}\" has been added.";
            return RedirectToAction(nameof(SubscriptionPlans), new { q });
        }

        // POST: Edit Plan
        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
        public IActionResult EditPlan(string id, string name, decimal price, int durationMonths, string description, bool isActive, string q = "")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToDashboard();

            PlanItem? p = null;

            if (p == null)
            {
                TempData["Error"] = "Plan not found.";
                return RedirectToAction(nameof(SubscriptionPlans), new { q });
            }

            name = (name ?? "").Trim();
            description = (description ?? "").Trim();

            if (!string.IsNullOrWhiteSpace(name)) p.Name = name;
            if (price >= 0) p.Price = price;
            if (durationMonths > 0) p.DurationMonths = durationMonths;
            p.Description = description;
            p.IsActive = isActive;

            TempData["Success"] = $"\"{p.Name}\" has been updated.";
            return RedirectToAction(nameof(SubscriptionPlans), new { q });
        }

        // POST: Archive Plan
        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
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

        // GET: /Home/Login
        [HttpGet]
        public IActionResult Login()
        {
            // If already logged in, go dashboard
            if (IsLoggedIn())
                return RedirectToDashboard();

            return View(new LoginViewModel());
        }

        // ✅ UPDATED: POST /Home/Login (Identity DB login)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.ErrorMessage = "Please fill up all required fields.";
                return View(model);
            }

            model.Email = (model.Email ?? "").Trim();
            model.Password = (model.Password ?? "").Trim();

            Console.WriteLine($"[Login] Attempting login for: {model.Email}");

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                Console.WriteLine($"[Login] User NOT found: {model.Email}");
                model.ErrorMessage = "Invalid email or password.";
                return View(model);
            }

            Console.WriteLine($"[Login] User found: {user.Email}, UserName: {user.UserName}");

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!,
                model.Password,
                isPersistent: false,
                lockoutOnFailure: false
            );

            Console.WriteLine($"[Login] SignIn result: Succeeded={result.Succeeded}, IsLockedOut={result.IsLockedOut}, IsNotAllowed={result.IsNotAllowed}, RequiresTwoFactor={result.RequiresTwoFactor}");

            if (!result.Succeeded)
            {
                model.ErrorMessage = "Invalid email or password.";
                return View(model);
            }

            // Get user roles from Identity
            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "";

            // Load BarangayId from BusinessUsers table
            var businessUser = await _context.BusinessUsers
                .FirstOrDefaultAsync(u => u.Email.ToLower() == model.Email.ToLower() && u.IsActive);

            int? barangayId = businessUser?.BarangayId;
            string barangayName = businessUser?.BarangayName ?? "";

            // Add BarangayId as claim
            await AddBarangayClaimAsync(user, barangayId);

            // Save to session
            HttpContext.Session.SetString("UserId", businessUser?.Id.ToString() ?? "");
            HttpContext.Session.SetString("UserName", user.Email ?? "User");
            HttpContext.Session.SetString("Role", role);
            HttpContext.Session.SetString("RoleLabel", GetRoleLabel(role));
            HttpContext.Session.SetString("BarangayId", barangayId?.ToString() ?? "");
            HttpContext.Session.SetString("Barangay", barangayName);

            Console.WriteLine($"[Login] Role: {role}, BarangayId: {barangayId}, BarangayName: {barangayName}");

            // Redirect based on role
            if (role == "super_admin")
            {
                return RedirectToAction("System", "Dashboard"); // System dashboard
            }
            else
            {
                return RedirectToAction("Barangay", "Dashboard"); // Barangay dashboard
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

            // Super_admin cannot access barangay modules - redirect to system dashboard
            if (IsSuperAdmin()) return RedirectToAction("System", "Dashboard");

            var role = HttpContext.Session.GetString("Role") ?? "";
            var canUpload = role == "barangay_secretary" || role == "barangay_admin" || role == "barangay_staff";
            var canApprove = role == "barangay_admin";
            var canArchive = role == "barangay_admin" || role == "super_admin";

            q = (q ?? "").Trim().ToLower();
            category = string.IsNullOrWhiteSpace(category) ? "All Categories" : category.Trim();
            status = string.IsNullOrWhiteSpace(status) ? "all" : status.Trim().ToLower();
            archiveStatus = (archiveStatus ?? "active").Trim().ToLower();

            // TENANT FILTERING: filter by user's barangay only
            var barangayId = GetCurrentBarangayId();
            var query = _context.KnowledgeDocuments
                .Where(d => d.IsActive)
                .Where(d => d.BarangayId == barangayId)
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
                SuccessMessage = TempData["Success"] as string,
                ErrorMessage = TempData["Error"] as string,
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDoc(string title, string category, string tags, string description, IFormFile? file)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            var role = HttpContext.Session.GetString("Role") ?? "";
            var canUpload = role == "barangay_secretary" || role == "barangay_admin" || role == "barangay_staff";
            if (!canUpload) return RedirectToAction(nameof(KnowledgeRepository));

            title = (title ?? "").Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                TempData["Error"] = "Title is required.";
                return RedirectToAction(nameof(KnowledgeRepository));
            }

            // Get uploading user ID from session email
            var userEmail = HttpContext.Session.GetString("UserName") ?? "";
            var uploaderId = await _context.BusinessUsers
                .Where(u => u.Email == userEmail)
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            if (uploaderId == 0)
            {
                // Create a default entry if user not found
                uploaderId = 1;
            }

            // Handle file upload
            string? filePath = null;
            string? fileName = null;
            long? fileSize = null;
            string? fileType = null;

            if (file != null && file.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "documents");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                fileName = file.FileName;
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

            TempData["Success"] = $"Document uploaded: \"{title}\"";
            return RedirectToAction(nameof(KnowledgeRepository));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDoc(string id, string title, string category, string tags, string description, IFormFile? file)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            var role = HttpContext.Session.GetString("Role") ?? "";
            var canUpload = role == "barangay_secretary" || role == "barangay_admin" || role == "barangay_staff";
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

            // TENANT OWNERSHIP VALIDATION
            if (!IsSuperAdmin() && doc.BarangayId != GetCurrentBarangayId())
            {
                TempData["Error"] = "You cannot edit documents from another barangay.";
                return RedirectToAction(nameof(KnowledgeRepository));
            }

            // Handle file upload (replace existing if new file provided)
            if (file != null && file.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "documents");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var fileName = file.FileName;
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

            TempData["Success"] = "Document updated.";
            return RedirectToAction(nameof(KnowledgeRepository));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
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
                    // TENANT OWNERSHIP VALIDATION
                    if (!IsSuperAdmin() && doc.BarangayId != GetCurrentBarangayId())
                    {
                        TempData["Error"] = "You cannot archive documents from another barangay.";
                        return RedirectToAction(nameof(KnowledgeRepository));
                    }

                    doc.IsArchived = true;
                    doc.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                }
            }

            TempData["Success"] = "Document archived.";
            return RedirectToAction(nameof(KnowledgeRepository));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
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
                    if (!IsSuperAdmin() && doc.BarangayId != GetCurrentBarangayId())
                    {
                        TempData["Error"] = "You cannot restore documents from another barangay.";
                        return RedirectToAction(nameof(KnowledgeRepository));
                    }

                    doc.IsArchived = false;
                    doc.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
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
                    // TENANT OWNERSHIP VALIDATION
                    if (!IsSuperAdmin() && doc.BarangayId != GetCurrentBarangayId())
                    {
                        TempData["Error"] = "You cannot approve documents from another barangay.";
                        return RedirectToAction(nameof(KnowledgeRepository));
                    }

                    doc.Status = "approved";
                    doc.ApprovedAt = DateTime.Now;
                    doc.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
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
                    // TENANT OWNERSHIP VALIDATION
                    if (!IsSuperAdmin() && doc.BarangayId != GetCurrentBarangayId())
                    {
                        TempData["Error"] = "You cannot reject documents from another barangay.";
                        return RedirectToAction(nameof(KnowledgeRepository));
                    }

                    doc.Status = "rejected";
                    doc.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
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

            // Super_admin cannot access barangay modules - redirect to system dashboard
            if (IsSuperAdmin()) return RedirectToAction("System", "Dashboard");

            var role = HttpContext.Session.GetString("Role") ?? "";
            var canCreate = role == "barangay_secretary" || role == "barangay_admin" || role == "barangay_staff";
            var canApprove = role == "barangay_admin";
            var canArchive = role == "barangay_admin" || role == "super_admin";

            status = (status ?? "all").Trim().ToLower();
            q = (q ?? "").Trim();
            archiveStatus = (archiveStatus ?? "active").Trim().ToLower();

            var query = _context.Policies
                .Where(p => p.IsActive)
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

            // Get counts from all active policies
            var allPolicies = await _context.Policies.Where(p => p.IsActive).ToListAsync();

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
        public async Task<IActionResult> CreatePolicy(string title, string description, string status = "all", string q = "")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            var role = HttpContext.Session.GetString("Role") ?? "";
            var canCreate = role == "barangay_secretary" || role == "barangay_admin" || role == "barangay_staff";
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

            var policy = new Policy
            {
                Title = title,
                Description = description,
                Status = "draft",
                Version = "1.0",
                AuthorId = authorId,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.Policies.Add(policy);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Policy \"{title}\" created successfully.";
            return RedirectToAction(nameof(PoliciesManagement), new { status, q });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPolicy(string id, string title, string description, string status = "all", string q = "")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            var role = HttpContext.Session.GetString("Role") ?? "";
            var canEdit = role == "barangay_secretary" || role == "barangay_admin" || role == "barangay_staff";
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

            policy.Title = (title ?? policy.Title).Trim();
            policy.Description = (description ?? "").Trim();
            policy.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Policy updated successfully.";
            return RedirectToAction(nameof(PoliciesManagement), new { status, q });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
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
                    policy.IsArchived = true;
                    policy.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                }
            }

            TempData["Success"] = "Policy archived.";
            return RedirectToAction(nameof(PoliciesManagement), new { status, q });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
        public async Task<IActionResult> RestorePolicy(string id, string status = "all", string q = "")
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
                    policy.IsArchived = false;
                    policy.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
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
                }
            }

            TempData["Success"] = $"Policy status set to {newStatus}.";
            return RedirectToAction(nameof(PoliciesManagement), new { status, q });
        }

        // GET: /Home/LessonsLearned
        public async Task<IActionResult> LessonsLearned(string q = "", string dateFilter = "", string archiveStatus = "active")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            // Super_admin cannot access barangay modules - redirect to system dashboard
            if (IsSuperAdmin()) return RedirectToAction("System", "Dashboard");

            var role = HttpContext.Session.GetString("Role") ?? "";
            var barangayId = HttpContext.Session.GetInt32("BarangayId");
            bool canSubmit = role == "barangay_staff" || role == "barangay_secretary" || role == "barangay_admin";
            bool canModify = role == "barangay_admin" || role == "barangay_secretary";
            bool canArchive = role == "barangay_admin" || role == "super_admin";

            q = (q ?? "").Trim().ToLower();
            dateFilter = (dateFilter ?? "").Trim();
            archiveStatus = (archiveStatus ?? "active").Trim().ToLower();

            var query = _context.LessonsLearned
                .Where(l => l.BarangayId == barangayId);

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

            // Available dates for filter
            var availableDates = await _context.LessonsLearned
                .Where(l => !l.IsArchived && l.BarangayId == barangayId)
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

            var vm = new LessonsLearnedViewModel
            {
                CanSubmit = canSubmit,
                CanModify = canModify,
                CanArchive = canArchive,
                TotalLessons = await _context.LessonsLearned.CountAsync(l => !l.IsArchived && l.BarangayId == barangayId),
                RecentLessons = await _context.LessonsLearned.CountAsync(l => !l.IsArchived && l.BarangayId == barangayId && l.DateRecorded >= DateTime.Now.AddDays(-30)),
                ArchivedLessons = await _context.LessonsLearned.CountAsync(l => l.IsArchived && l.BarangayId == barangayId),
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
        public async Task<IActionResult> CreateLesson(string title, string problem, string actionTaken, string result, string recommendation)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            var role = HttpContext.Session.GetString("Role") ?? "";
            if (role != "barangay_admin" && role != "barangay_secretary" && role != "barangay_staff")
                return RedirectToAction(nameof(LessonsLearned));

            var barangayId = HttpContext.Session.GetInt32("BarangayId");
            var userId = HttpContext.Session.GetInt32("UserId") ?? 0;

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(problem) ||
                string.IsNullOrWhiteSpace(actionTaken) || string.IsNullOrWhiteSpace(result))
            {
                TempData["Error"] = "Title, Problem, Action Taken, and Result are required.";
                return RedirectToAction(nameof(LessonsLearned));
            }

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
                Status = "approved",
                CreatedAt = DateTime.Now,
                IsArchived = false
            };

            _context.LessonsLearned.Add(lesson);
            await _context.SaveChangesAsync();

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

            TempData["Success"] = "Lesson has been updated.";
            return RedirectToAction(nameof(LessonsLearned));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
        public async Task<IActionResult> ArchiveLesson(int id)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            var role = HttpContext.Session.GetString("Role") ?? "";
            if (role != "barangay_admin" && role != "super_admin")
                return RedirectToAction(nameof(LessonsLearned));

            var lesson = await _context.LessonsLearned.FindAsync(id);
            if (lesson != null)
            {
                lesson.IsArchived = true;
                lesson.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Lesson has been archived.";
            }

            return RedirectToAction(nameof(LessonsLearned));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
        public async Task<IActionResult> RestoreLesson(int id)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            var role = HttpContext.Session.GetString("Role") ?? "";
            if (role != "barangay_admin" && role != "super_admin")
                return RedirectToAction(nameof(LessonsLearned));

            var lesson = await _context.LessonsLearned.FindAsync(id);
            if (lesson != null)
            {
                lesson.IsArchived = false;
                lesson.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Lesson has been restored.";
            }

            return RedirectToAction(nameof(LessonsLearned), new { archiveStatus = "active" });
        }

        // GET: /Home/BestPractices
        [HttpGet]
        public async Task<IActionResult> BestPractices(string q = "", string status = "", string archiveStatus = "active")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            // Super_admin cannot access barangay modules - redirect to system dashboard
            if (IsSuperAdmin()) return RedirectToAction("System", "Dashboard");

            q = (q ?? "").Trim().ToLower();
            status = (status ?? "").Trim();
            archiveStatus = (archiveStatus ?? "active").Trim().ToLower();

            var role = HttpContext.Session.GetString("Role") ?? "";
            var barangayId = HttpContext.Session.GetInt32("BarangayId");
            bool canManage = role == "barangay_admin" || role == "barangay_secretary";
            bool canModify = canManage;
            bool canArchive = role == "barangay_admin" || role == "super_admin";

            var query = _context.BestPractices
                .Where(p => p.BarangayId == barangayId);

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
                    CreatedAt = p.CreatedAt
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
                TotalPractices = await _context.BestPractices.CountAsync(p => !p.IsArchived && p.BarangayId == barangayId),
                ActivePractices = await _context.BestPractices.CountAsync(p => !p.IsArchived && p.BarangayId == barangayId && p.Status == "Active"),
                ArchivedPractices = await _context.BestPractices.CountAsync(p => p.IsArchived && p.BarangayId == barangayId),
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
        public async Task<IActionResult> CreatePractice(string title, string purpose, string steps, string resourcesNeeded, string ownerOffice, string category)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            var role = HttpContext.Session.GetString("Role") ?? "";
            if (role != "barangay_admin" && role != "barangay_secretary")
                return RedirectToAction(nameof(BestPractices));

            var barangayId = HttpContext.Session.GetInt32("BarangayId");
            var userId = HttpContext.Session.GetInt32("UserId") ?? 0;

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(steps))
            {
                TempData["Error"] = "Title and Steps are required.";
                return RedirectToAction(nameof(BestPractices));
            }

            var practice = new BestPractice
            {
                Title = title.Trim(),
                Description = purpose?.Trim() ?? "",
                Purpose = purpose?.Trim(),
                Steps = steps.Trim(),
                ResourcesNeeded = resourcesNeeded?.Trim(),
                OwnerOffice = ownerOffice?.Trim(),
                Category = string.IsNullOrWhiteSpace(category) ? "Governance" : category.Trim(),
                Status = "Active",
                BarangayId = barangayId,
                SubmittedById = userId,
                CreatedAt = DateTime.Now,
                IsArchived = false
            };

            _context.BestPractices.Add(practice);
            await _context.SaveChangesAsync();

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
            if (role != "barangay_admin" && role != "barangay_secretary")
                return RedirectToAction(nameof(BestPractices));

            var practice = await _context.BestPractices.FindAsync(id);
            if (practice == null || practice.IsArchived)
            {
                TempData["Error"] = "Practice not found.";
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

            TempData["Success"] = "Best practice has been updated.";
            return RedirectToAction(nameof(BestPractices));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
        public async Task<IActionResult> ArchivePractice(int id)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            var role = HttpContext.Session.GetString("Role") ?? "";
            if (role != "barangay_admin" && role != "super_admin")
                return RedirectToAction(nameof(BestPractices));

            var practice = await _context.BestPractices.FindAsync(id);
            if (practice != null)
            {
                practice.IsArchived = true;
                practice.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Practice has been archived.";
            }

            return RedirectToAction(nameof(BestPractices));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
        public async Task<IActionResult> RestorePractice(int id)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            var role = HttpContext.Session.GetString("Role") ?? "";
            if (role != "barangay_admin" && role != "super_admin")
                return RedirectToAction(nameof(BestPractices));

            var practice = await _context.BestPractices.FindAsync(id);
            if (practice != null)
            {
                practice.IsArchived = false;
                practice.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Practice has been restored.";
            }

            return RedirectToAction(nameof(BestPractices), new { archiveStatus = "active" });
        }

        // GET: /Home/KnowledgeSharing
        [HttpGet]
        public async Task<IActionResult> KnowledgeSharing(string q = "", string category = "All Categories", string archiveStatus = "active")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            // Super_admin cannot access barangay modules - redirect to system dashboard
            if (IsSuperAdmin()) return RedirectToAction("System", "Dashboard");

            var role = HttpContext.Session.GetString("Role") ?? "";
            var barangayId = GetCurrentBarangayId();

            bool canPost = role == "barangay_staff" || role == "barangay_secretary" || role == "barangay_admin";
            bool canAnnounce = role == "barangay_admin";
            bool canArchive = role == "barangay_admin" || role == "super_admin";

            q = (q ?? "").Trim().ToLower();
            category = string.IsNullOrWhiteSpace(category) ? "All Categories" : category.Trim();
            archiveStatus = (archiveStatus ?? "active").Trim().ToLower();

            // Query discussions
            var query = _context.KnowledgeDiscussions
                .Where(d => d.IsActive)
                .Where(d => d.BarangayId == barangayId)
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
                ActiveMembers = new List<string>(),
                Categories = new List<string> { "All Categories", "Health", "Environment", "Youth", "Education", "Governance", "Finance" },
                MembersOnline = 0,
                TotalDiscussions = await _context.KnowledgeDiscussions.CountAsync(d => d.IsActive && !d.IsArchived && d.BarangayId == barangayId),
                ArchivedDiscussions = await _context.KnowledgeDiscussions.CountAsync(d => d.IsActive && d.IsArchived && d.BarangayId == barangayId),
                SuccessMessage = TempData["Success"] as string,
                ErrorMessage = TempData["Error"] as string
            };

            return View(vm);
        }

        // POST: Create Discussion
        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
        public async Task<IActionResult> CreateDiscussion(string title, string content, string category)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            var role = HttpContext.Session.GetString("Role") ?? "";
            bool canPost = role == "barangay_staff" || role == "barangay_secretary" || role == "barangay_admin";
            if (!canPost) return RedirectToAction(nameof(KnowledgeSharing));

            title = (title ?? "").Trim();
            content = (content ?? "").Trim();
            category = string.IsNullOrWhiteSpace(category) ? "General" : category.Trim();

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
            {
                TempData["Error"] = "Title and content are required.";
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

            TempData["Success"] = $"Discussion \"{title}\" created successfully.";
            return RedirectToAction(nameof(KnowledgeSharing));
        }

        // POST: Edit Discussion
        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
        public async Task<IActionResult> EditDiscussion(string id, string title, string content, string category)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            var role = HttpContext.Session.GetString("Role") ?? "";
            bool canEdit = role == "barangay_staff" || role == "barangay_secretary" || role == "barangay_admin";
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
            if (!IsSuperAdmin() && discussion.BarangayId != GetCurrentBarangayId())
            {
                TempData["Error"] = "You cannot edit discussions from another barangay.";
                return RedirectToAction(nameof(KnowledgeSharing));
            }

            discussion.Title = (title ?? discussion.Title).Trim();
            discussion.Content = (content ?? discussion.Content).Trim();
            discussion.Category = string.IsNullOrWhiteSpace(category) ? discussion.Category : category.Trim();
            discussion.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Discussion updated successfully.";
            return RedirectToAction(nameof(KnowledgeSharing));
        }

        // POST: Archive Discussion
        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
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
                    if (!IsSuperAdmin() && discussion.BarangayId != GetCurrentBarangayId())
                    {
                        TempData["Error"] = "You cannot archive discussions from another barangay.";
                        return RedirectToAction(nameof(KnowledgeSharing));
                    }

                    discussion.IsArchived = true;
                    discussion.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Discussion archived.";
                }
            }

            return RedirectToAction(nameof(KnowledgeSharing));
        }

        // POST: Restore Discussion
        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
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
                    if (!IsSuperAdmin() && discussion.BarangayId != GetCurrentBarangayId())
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

        // POST: Quick Post (simplified create)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickPostKnowledge(string content)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            var role = HttpContext.Session.GetString("Role") ?? "";
            bool canPost = role == "barangay_staff" || role == "barangay_secretary" || role == "barangay_admin";
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

        public async Task<IActionResult> UserManagement()
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToDashboard();

            var dbUsers = await _context.BusinessUsers
                .Where(u => u.IsActive)
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

            var vm = new UserManagementViewModel
            {
                Users = users
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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

            // Check if email already exists
            var exists = await _context.BusinessUsers.AnyAsync(u => u.Email == email);
            if (exists)
            {
                TempData["Error"] = "A user with this email already exists.";
                return RedirectToAction(nameof(UserManagement));
            }

            // Hash password (simplified - in production use proper hashing)
            var passwordHash = Convert.ToBase64String(
                System.Security.Cryptography.SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(password)
                )
            );

            var user = new Models.Entities.User
            {
                FullName = name,
                Email = email,
                PasswordHash = passwordHash,
                Role = role,
                BarangayName = barangay,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.BusinessUsers.Add(user);
            await _context.SaveChangesAsync();

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

            await _context.SaveChangesAsync();

            TempData["Success"] = $"User \"{user.FullName}\" updated successfully.";
            return RedirectToAction(nameof(UserManagement));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
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
                    TempData["Success"] = $"User \"{user.FullName}\" archived.";
                }
            }

            return RedirectToAction(nameof(UserManagement));
        }

        // GET: /Home/Announcements
        [HttpGet]
        public async Task<IActionResult> Announcements(string filter = "all", string archiveStatus = "active")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            // Super_admin cannot access barangay modules - redirect to system dashboard
            if (IsSuperAdmin()) return RedirectToAction("System", "Dashboard");

            var role = HttpContext.Session.GetString("Role") ?? "";
            var barangayId = GetCurrentBarangayId();

            bool canCreate = role == "barangay_admin" || role == "barangay_secretary" || role == "barangay_staff";
            bool canArchive = role == "barangay_admin" || role == "super_admin";

            filter = (filter ?? "all").Trim().ToLower();
            archiveStatus = (archiveStatus ?? "active").Trim().ToLower();

            var query = _context.Announcements
                .Where(a => a.IsActive)
                .Where(a => a.BarangayId == barangayId)
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

            // Get counts from all active announcements for this barangay
            var allAnnouncements = await _context.Announcements
                .Where(a => a.IsActive && a.BarangayId == barangayId)
                .ToListAsync();

            var vm = new AnnouncementsViewModel
            {
                Filter = filter,
                ArchiveStatus = archiveStatus,
                CanCreate = canCreate,
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
        public async Task<IActionResult> CreateAnnouncement(string title, string content, string priority, string status, string filter = "all")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            var role = HttpContext.Session.GetString("Role") ?? "";
            var canCreate = role == "barangay_admin" || role == "barangay_secretary" || role == "barangay_staff";
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
            var canEdit = role == "barangay_admin" || role == "barangay_secretary" || role == "barangay_staff";
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

            // TENANT OWNERSHIP VALIDATION
            if (!IsSuperAdmin() && announcement.BarangayId != GetCurrentBarangayId())
            {
                TempData["Error"] = "You cannot edit announcements from another barangay.";
                return RedirectToAction(nameof(Announcements), new { filter });
            }

            announcement.Title = (title ?? announcement.Title).Trim();
            announcement.Content = (content ?? "").Trim();
            announcement.Priority = string.IsNullOrWhiteSpace(priority) ? announcement.Priority : priority.Trim().ToLower();
            
            // If changing to published, set PublishedAt
            if (status == "published" && announcement.Status != "published")
            {
                announcement.PublishedAt = DateTime.Now;
            }
            announcement.Status = string.IsNullOrWhiteSpace(status) ? announcement.Status : status.Trim().ToLower();
            announcement.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Announcement updated successfully.";
            return RedirectToAction(nameof(Announcements), new { filter });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
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
                    // TENANT OWNERSHIP VALIDATION
                    if (!IsSuperAdmin() && announcement.BarangayId != GetCurrentBarangayId())
                    {
                        TempData["Error"] = "You cannot archive announcements from another barangay.";
                        return RedirectToAction(nameof(Announcements), new { filter });
                    }

                    announcement.IsArchived = true;
                    announcement.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                }
            }

            TempData["Success"] = "Announcement archived.";
            return RedirectToAction(nameof(Announcements), new { filter });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
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
                    // TENANT OWNERSHIP VALIDATION
                    if (!IsSuperAdmin() && announcement.BarangayId != GetCurrentBarangayId())
                    {
                        TempData["Error"] = "You cannot restore announcements from another barangay.";
                        return RedirectToAction(nameof(Announcements), new { filter });
                    }

                    announcement.IsArchived = false;
                    announcement.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
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
        public IActionResult AuditLogs(string q = "", string module = "all")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToDashboard();

            q = (q ?? "").Trim().ToLower();
            module = (module ?? "all").Trim();

            var allLogs = new List<LogItem>();
            var list = allLogs;

            if (!string.IsNullOrWhiteSpace(q))
            {
                list = list.Where(l =>
                    (l.User ?? "").ToLower().Contains(q) ||
                    (l.Target ?? "").ToLower().Contains(q) ||
                    (l.Action ?? "").ToLower().Contains(q)
                ).ToList();
            }

            if (module != "all")
            {
                list = list.Where(l => l.Module == module).ToList();
            }

            var vm = new AuditLogsViewModel
            {
                SearchQuery = q,
                ModuleFilter = module,
                Logs = list
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DenyViewOnly]
        public async Task<IActionResult> ArchiveLog(string id, string q = "", string module = "all")
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
            return RedirectToAction(nameof(AuditLogs), new { q, module });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ClearAllLogs()
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToDashboard();

            return RedirectToAction(nameof(AuditLogs));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ExportLogsCsv(string q = "", string module = "all")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToDashboard();

            q = (q ?? "").Trim().ToLower();
            module = (module ?? "all").Trim();

            var allLogs = new List<LogItem>();
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
        public IActionResult Settings(string tab = "general")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToDashboard();

            tab = (tab ?? "general").Trim().ToLower();

            var vm = new SettingsViewModel
            {
                Tab = tab,

                FullName = HttpContext.Session.GetString("Settings_FullName") ?? (HttpContext.Session.GetString("UserName") ?? ""),
                Email = HttpContext.Session.GetString("Settings_Email") ?? "",
                Barangay = HttpContext.Session.GetString("Settings_Barangay") ?? (HttpContext.Session.GetString("Barangay") ?? ""),
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
        public IActionResult SaveProfile(SettingsViewModel model)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToDashboard();

            HttpContext.Session.SetString("Settings_FullName", (model.FullName ?? "").Trim());
            HttpContext.Session.SetString("Settings_Email", (model.Email ?? "").Trim());
            HttpContext.Session.SetString("Settings_Barangay", (model.Barangay ?? "").Trim());
            HttpContext.Session.SetString("Settings_Language", (model.Language ?? "en").Trim());

            if (!string.IsNullOrWhiteSpace(model.FullName))
                HttpContext.Session.SetString("UserName", model.FullName.Trim());

            if (!string.IsNullOrWhiteSpace(model.Barangay))
                HttpContext.Session.SetString("Barangay", model.Barangay.Trim());

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
        public IActionResult UpdatePassword(string currentPassword, string newPassword, string confirmPassword)
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

            TempData["Success"] = "Password updated. Your password has been changed.";
            return RedirectToAction(nameof(Settings), new { tab = "security" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleTwoFa(bool twoFaEnabled)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToDashboard();

            HttpContext.Session.SetString("Settings_TwoFa", twoFaEnabled ? "true" : "false");
            TempData["Success"] = twoFaEnabled ? "2FA Enabled." : "2FA Disabled.";
            return RedirectToAction(nameof(Settings), new { tab = "security" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveSystem(SettingsViewModel model)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToDashboard();

            HttpContext.Session.SetString("Settings_Maintenance", model.MaintenanceMode ? "true" : "false");
            HttpContext.Session.SetString("Settings_SessionTimeout", model.SessionTimeout ?? "30");
            HttpContext.Session.SetString("Settings_DocFormat", model.DocFormat ?? "pdf");

            TempData["Success"] = "System settings saved. System preferences have been updated.";
            return RedirectToAction(nameof(Settings), new { tab = "system" });
        }

        [Authorize(Roles = "super_admin,barangay_admin")]
        public IActionResult PasswordRequests()
        {
            return View();
        }

        // GET: /Home/ForgotPassword
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        // POST: /Home/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Submitted = false;
                return View(model);
            }

            model.Submitted = true;
            model.SuccessMessage = "If your email is registered, you will receive reset instructions.";

            return View(model);
        }

        // ✅ UPDATED: /Home/Logout (clears session + identity cookie)
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Index));
        }

        // =============================================
        // BARANGAYS MANAGEMENT (super_admin only)
        // =============================================

        [HttpGet]
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

            return RedirectToAction("BarangaysManagement", new { q });
        }

        [HttpPost]
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
            }

            return RedirectToAction("BarangaysManagement", new { q });
        }

        [HttpPost]
        public async Task<IActionResult> ArchiveBarangay(int id, string q = "")
        {
            if (!IsSuperAdmin()) return RedirectToDashboard();

            var barangay = await _context.Barangays.FindAsync(id);
            if (barangay != null)
            {
                barangay.IsActive = false;
                barangay.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("BarangaysManagement", new { q });
        }

        // =============================================
        // KNOWLEDGE DISCUSSIONS (barangay module)
        // =============================================

        [HttpGet]
        public async Task<IActionResult> KnowledgeDiscussions(string q = "", string category = "All Categories")
        {
            if (IsSuperAdmin()) return RedirectToAction("System", "Dashboard");

            var role = GetCurrentRole();
            var barangayId = GetCurrentBarangayId();
            var canModify = role == "barangay_admin" || role == "barangay_secretary" || role == "barangay_staff";

            var discussions = await _context.KnowledgeDiscussions
                .Include(d => d.Author)
                .Where(d => d.IsActive)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            // Filter by barangay
            if (barangayId.HasValue)
            {
                discussions = discussions.Where(d => d.BarangayId == barangayId.Value).ToList();
            }

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
        public async Task<IActionResult> CreateDiscussion(string title, string content, string? category, string q = "", string categoryFilter = "All Categories")
        {
            if (IsSuperAdmin()) return RedirectToAction("System", "Dashboard");

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

            return RedirectToAction("KnowledgeDiscussions", new { q, category = categoryFilter });
        }

        [HttpPost]
        [DenyViewOnly]
        public async Task<IActionResult> EditDiscussion(int id, string title, string content, string? category, string q = "", string categoryFilter = "All Categories")
        {
            if (IsSuperAdmin()) return RedirectToAction("System", "Dashboard");

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
        public async Task<IActionResult> ArchiveDiscussion(int id, string q = "", string categoryFilter = "All Categories")
        {
            if (IsSuperAdmin()) return RedirectToAction("System", "Dashboard");

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
        public async Task<IActionResult> RestoreDiscussion(int id, string q = "", string categoryFilter = "All Categories")
        {
            if (IsSuperAdmin()) return RedirectToAction("System", "Dashboard");

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
