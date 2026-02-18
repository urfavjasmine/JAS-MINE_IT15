using JAS_MINE_IT15.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JAS_MINE_IT15.Controllers
{
    public class HomeController : Controller
    {
        // TODO: Inject DbContext via constructor for database integration
        // private readonly ApplicationDbContext _context;
        // public HomeController(ApplicationDbContext context) { _context = context; }

        private bool IsLoggedIn() =>
            !string.IsNullOrEmpty(HttpContext.Session.GetString("UserName"));

        private bool IsAdminRole()
        {
            var role = HttpContext.Session.GetString("Role");
            return role == "super_admin" || role == "barangay_admin";
        }

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
                return RedirectToAction(nameof(DashboardHome));

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
            if (!IsAdminRole()) return RedirectToAction(nameof(DashboardHome));

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
        public IActionResult CreateSubscription(string barangayName, string planName, string startDate, string endDate, string q = "", string status = "all")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToAction(nameof(DashboardHome));

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
            // var subscription = new SubscriptionItem { ... };
            // _context.Subscriptions.Add(subscription);
            // _context.SaveChanges();

            TempData["Success"] = $"{planName} assigned to {barangayName}.";
            return RedirectToAction(nameof(BarangaySubscriptions), new { q, status });
        }

        // POST: Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditSubscription(string id, string barangayName, string planName, string startDate, string endDate, string q = "", string status = "all")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToAction(nameof(DashboardHome));

            // TODO: Fetch subscription from database
            // var item = _context.Subscriptions.FirstOrDefault(x => x.Id == id);
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

            // TODO: Save changes to database
            // _context.SaveChanges();

            TempData["Success"] = $"Subscription for {item.BarangayName} updated.";
            return RedirectToAction(nameof(BarangaySubscriptions), new { q, status });
        }

        // POST: Cancel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CancelSubscription(string id, string q = "", string status = "all")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToAction(nameof(DashboardHome));

            // TODO: Fetch and update subscription in database
            // var item = _context.Subscriptions.FirstOrDefault(x => x.Id == id);
            // if (item != null) { item.Status = "Cancelled"; _context.SaveChanges(); }

            TempData["Success"] = "Subscription cancelled.";
            return RedirectToAction(nameof(BarangaySubscriptions), new { q, status });
        }

        // GET: /Home/MySubscription
        [HttpGet]
        public IActionResult MySubscription()
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            var barangay = HttpContext.Session.GetString("Barangay") ?? "Your Barangay";

            // TODO: Fetch subscription and payments from database based on current user's barangay
            // var subscription = _context.Subscriptions.FirstOrDefault(s => s.BarangayName == barangay);
            // var payments = _context.Payments.Where(p => p.BarangayName == barangay).ToList();

            var vm = new MySubscriptionViewModel
            {
                BarangayName = barangay,
                Subscription = null, // No subscription data available - will be populated from database
                Payments = new List<MySubscriptionViewModel.PaymentRow>()
            };

            return View(vm);
        }

        // GET: /Home/SubscriptionPayments
        [HttpGet]
        public IActionResult SubscriptionPayments(string q = "")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToAction(nameof(DashboardHome));

            q = (q ?? "").Trim();

            // TODO: Fetch payments from database
            // var allPayments = _context.Payments.ToList();
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

            // TODO: Fetch dropdown lists from database
            // var barangays = _context.Barangays.Select(b => b.Name).ToList();
            // var plans = _context.Plans.Select(p => p.Name).ToList();
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
        public IActionResult CreatePayment(string barangayName, string planName, decimal amount, string paymentDate, string paymentMethod, string status, string q = "")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToAction(nameof(DashboardHome));

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

            // TODO: Add payment to database
            // var payment = new PaymentItem { ... };
            // _context.Payments.Add(payment);
            // _context.SaveChanges();

            TempData["Success"] = $"Payment of ₱{amount:N0} recorded.";
            return RedirectToAction(nameof(SubscriptionPayments), new { q });
        }

        // POST: Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditPayment(string id, string barangayName, string planName, decimal amount, string paymentDate, string paymentMethod, string status, string q = "")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToAction(nameof(DashboardHome));

            // TODO: Fetch payment from database
            // var p = _context.Payments.FirstOrDefault(x => x.Id == id);
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

            // TODO: Save changes to database
            // _context.SaveChanges();

            TempData["Success"] = "Payment record has been updated.";
            return RedirectToAction(nameof(SubscriptionPayments), new { q });
        }

        // POST: Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePayment(string id, string q = "")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToAction(nameof(DashboardHome));

            // TODO: Delete payment from database
            // var payment = _context.Payments.FirstOrDefault(x => x.Id == id);
            // if (payment != null) { _context.Payments.Remove(payment); _context.SaveChanges(); }

            TempData["Success"] = "Payment deleted.";
            return RedirectToAction(nameof(SubscriptionPayments), new { q });
        }

        // GET: /Home/SubscriptionPlans
        [HttpGet]
        public IActionResult SubscriptionPlans(string q = "")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToAction(nameof(DashboardHome));

            q = (q ?? "").Trim();

            // TODO: Fetch plans from database
            // var allPlans = _context.Plans.ToList();
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
        public IActionResult CreatePlan(string name, decimal price, int durationMonths, string description, bool isActive, string q = "")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToAction(nameof(DashboardHome));

            name = (name ?? "").Trim();
            description = (description ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "Plan name is required.";
                return RedirectToAction(nameof(SubscriptionPlans), new { q });
            }

            if (durationMonths <= 0) durationMonths = 1;
            if (price < 0) price = 0;

            // TODO: Add plan to database
            // var plan = new PlanItem { ... };
            // _context.Plans.Add(plan);
            // _context.SaveChanges();

            TempData["Success"] = $"\"{name}\" has been added.";
            return RedirectToAction(nameof(SubscriptionPlans), new { q });
        }

        // POST: Edit Plan
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditPlan(string id, string name, decimal price, int durationMonths, string description, bool isActive, string q = "")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToAction(nameof(DashboardHome));

            // TODO: Fetch plan from database
            // var p = _context.Plans.FirstOrDefault(x => x.Id == id);
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

            // TODO: Save changes to database
            // _context.SaveChanges();

            TempData["Success"] = $"\"{p.Name}\" has been updated.";
            return RedirectToAction(nameof(SubscriptionPlans), new { q });
        }

        // POST: Delete Plan
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePlan(string id, string q = "")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToAction(nameof(DashboardHome));

            // TODO: Fetch and delete plan from database
            // var plan = _context.Plans.FirstOrDefault(x => x.Id == id);
            // if (plan != null) { _context.Plans.Remove(plan); _context.SaveChanges(); TempData["Success"] = $"\"{plan.Name}\" removed."; }
            // else { TempData["Error"] = "Plan not found."; }

            TempData["Success"] = "Plan removed.";
            return RedirectToAction(nameof(SubscriptionPlans), new { q });
        }

        // GET: /Home/Login
        [HttpGet]
        public IActionResult Login()
        {
            // If already logged in, go dashboard
            if (IsLoggedIn())
                return RedirectToAction(nameof(DashboardHome));

            return View(new LoginViewModel());
        }

        // POST: /Home/Login 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.ErrorMessage = "Please fill up all required fields.";
                return View(model);
            }

            model.Email = (model.Email ?? "").Trim();
            model.Password = (model.Password ?? "").Trim();

            // TODO: Authenticate user against database
            // var user = _context.Users.FirstOrDefault(u => u.Email == model.Email);
            // if (user == null || !VerifyPassword(model.Password, user.PasswordHash))
            // {
            //     model.ErrorMessage = "Invalid email/password.";
            //     return View(model);
            // }
            // var role = user.Role;
            // var name = user.Name;
            // var barangay = user.Barangay ?? "Unknown Barangay";
            //
            // HttpContext.Session.SetString("UserName", name);
            // HttpContext.Session.SetString("Role", role);
            // HttpContext.Session.SetString("RoleLabel", GetRoleLabel(role));
            // HttpContext.Session.SetString("Barangay", barangay);
            // return RedirectToAction(nameof(DashboardHome));

            // For now, authentication is disabled without database
            model.ErrorMessage = "Authentication requires database integration.";
            return View(model);
        }

        // GET: /Home/DashboardHome
        [HttpGet]
        public IActionResult DashboardHome()
        {
            if (!IsLoggedIn())
                return RedirectToAction(nameof(Login));

            var vm = new DashboardHomeViewModel();

            vm.Role = HttpContext.Session.GetString("Role") ?? "";
            vm.RoleLabel = HttpContext.Session.GetString("RoleLabel") ?? "";

            // TODO: Fetch counts from database
            // vm.TotalDocuments = _context.Documents.Count();
            // vm.ActivePolicies = _context.Policies.Count(p => p.Status == "approved");
            // vm.LessonsLearned = _context.Lessons.Count();
            // vm.BestPractices = _context.Practices.Count();
            vm.TotalDocuments = 0;
            vm.ActivePolicies = 0;
            vm.LessonsLearned = 0;
            vm.BestPractices = 0;

            // TODO: Fetch module usage statistics from database
            vm.ModuleUsage = new List<ModuleUsageRow>();

            return View(vm);
        }

        // GET: /Home/KnowledgeRepository
        [HttpGet]
        public IActionResult KnowledgeRepository(string q = "", string category = "All Categories")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            var role = HttpContext.Session.GetString("Role") ?? "";
            var canUpload = role == "barangay_secretary" || role == "barangay_admin" || role == "super_admin";
            var canApprove = role == "barangay_admin" || role == "super_admin";

            q = (q ?? "").Trim().ToLower();
            category = string.IsNullOrWhiteSpace(category) ? "All Categories" : category.Trim();

            // TODO: Fetch documents from database
            // var allDocs = _context.Documents.OrderByDescending(d => d.Date).ToList();
            var allDocs = new List<RepoDocument>();

            var list = allDocs;

            if (!string.IsNullOrWhiteSpace(q))
            {
                list = list.Where(d =>
                    (d.Title ?? "").ToLower().Contains(q) ||
                    (d.TagsCsv ?? "").ToLower().Contains(q)
                ).ToList();
            }

            if (category != "All Categories")
                list = list.Where(d => d.Category == category).ToList();

            var vm = new KnowledgeRepositoryViewModel
            {
                SearchQuery = q,
                SelectedCategory = category,
                // TODO: Fetch categories from database
                Categories = new List<string> { "All Categories", "Resolutions", "Ordinances", "Memorandums", "Policies", "Reports" },
                Documents = list,
                CanUpload = canUpload,
                CanApprove = canApprove,
                SuccessMessage = TempData["Success"] as string,
                ErrorMessage = TempData["Error"] as string,
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateDoc(string title, string category, string tags, string description)
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

            var uploader = HttpContext.Session.GetString("UserName") ?? "Unknown";

            // TODO: Add document to database
            // var doc = new RepoDocument { ... };
            // _context.Documents.Add(doc);
            // _context.SaveChanges();

            TempData["Success"] = $"Document uploaded: \"{title}\"";
            return RedirectToAction(nameof(KnowledgeRepository));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditDoc(string id, string title, string category, string tags, string description)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            var role = HttpContext.Session.GetString("Role") ?? "";
            var canUpload = role == "barangay_secretary" || role == "barangay_admin" || role == "super_admin";
            if (!canUpload) return RedirectToAction(nameof(KnowledgeRepository));

            // TODO: Fetch and update document in database
            // var doc = _context.Documents.FirstOrDefault(d => d.Id == id);
            // if (doc != null) { ... _context.SaveChanges(); }

            TempData["Success"] = "Document updated.";
            return RedirectToAction(nameof(KnowledgeRepository));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteDoc(string id)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            var role = HttpContext.Session.GetString("Role") ?? "";
            var canUpload = role == "barangay_secretary" || role == "barangay_admin" || role == "super_admin";
            if (!canUpload) return RedirectToAction(nameof(KnowledgeRepository));

            // TODO: Delete document from database
            // var doc = _context.Documents.FirstOrDefault(d => d.Id == id);
            // if (doc != null) { _context.Documents.Remove(doc); _context.SaveChanges(); }

            TempData["Success"] = "Document deleted.";
            return RedirectToAction(nameof(KnowledgeRepository));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApproveDoc(string id)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            var role = HttpContext.Session.GetString("Role") ?? "";
            var canApprove = role == "barangay_admin" || role == "super_admin";
            if (!canApprove) return RedirectToAction(nameof(KnowledgeRepository));

            // TODO: Update document status in database
            // var doc = _context.Documents.FirstOrDefault(d => d.Id == id);
            // if (doc != null) { doc.Status = "approved"; _context.SaveChanges(); }

            TempData["Success"] = "Document approved.";
            return RedirectToAction(nameof(KnowledgeRepository));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RejectDoc(string id)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            var role = HttpContext.Session.GetString("Role") ?? "";
            var canApprove = role == "barangay_admin" || role == "super_admin";
            if (!canApprove) return RedirectToAction(nameof(KnowledgeRepository));

            // TODO: Update document status in database
            // var doc = _context.Documents.FirstOrDefault(d => d.Id == id);
            // if (doc != null) { doc.Status = "rejected"; _context.SaveChanges(); }

            TempData["Success"] = "Document rejected.";
            return RedirectToAction(nameof(KnowledgeRepository));
        }

        // GET: /Home/PoliciesProcedures
        [HttpGet]
        public IActionResult PoliciesManagement(string status = "all", string q = "")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            var role = HttpContext.Session.GetString("Role") ?? "";
            var canCreate = role == "barangay_secretary" || role == "barangay_admin" || role == "super_admin";
            var canApprove = role == "barangay_admin" || role == "super_admin";

            status = (status ?? "all").Trim().ToLower();
            q = (q ?? "").Trim();

            // TODO: Fetch policies from database
            // var allPolicies = _context.Policies.OrderByDescending(x => x.LastUpdated).ToList();
            var allPolicies = new List<PolicyItem>();

            var list = allPolicies;

            if (status != "all")
                list = list.Where(x => (x.Status ?? "").ToLower() == status).ToList();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var qq = q.ToLower();
                list = list.Where(x =>
                    (x.Title ?? "").ToLower().Contains(qq) ||
                    (x.Description ?? "").ToLower().Contains(qq)
                ).ToList();
            }

            var vm = new PoliciesManagementViewModel
            {
                StatusFilter = status,
                SearchQuery = q,
                CanCreate = canCreate,
                CanApprove = canApprove,

                CountAll = allPolicies.Count,
                CountApproved = allPolicies.Count(x => x.Status == "approved"),
                CountPending = allPolicies.Count(x => x.Status == "pending"),
                CountDraft = allPolicies.Count(x => x.Status == "draft"),

                Policies = list
            };

            return View(vm);
        }

        // This is used for Create action (adds to list with "draft" status)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreatePolicy(string title, string description, string status = "all", string q = "")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            title = (title ?? "").Trim();
            description = (description ?? "").Trim();

            if (string.IsNullOrWhiteSpace(title))
                return RedirectToAction(nameof(PoliciesManagement), new { status, q });

            // TODO: Add policy to database
            // var policy = new PolicyItem { ... };
            // _context.Policies.Add(policy);
            // _context.SaveChanges();

            return RedirectToAction(nameof(PoliciesManagement), new { status, q });
        }

        // This is used for Edit action (just updates title/description, not status)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditPolicy(string id, string title, string description, string status = "all", string q = "")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            // TODO: Fetch and update policy in database
            // var p = _context.Policies.FirstOrDefault(x => x.Id == id);
            // if (p != null) { p.Title = ...; p.Description = ...; p.LastUpdated = ...; _context.SaveChanges(); }

            return RedirectToAction(nameof(PoliciesManagement), new { status, q });
        }

        // This is used for Delete action (removes from list)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePolicy(string id, string status = "all", string q = "")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            // TODO: Delete policy from database
            // var p = _context.Policies.FirstOrDefault(x => x.Id == id);
            // if (p != null) { _context.Policies.Remove(p); _context.SaveChanges(); }

            return RedirectToAction(nameof(PoliciesManagement), new { status, q });
        }

        // This is used for Approve/Reject actions (just change status)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SetPolicyStatus(string id, string newStatus, string status = "all", string q = "")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            newStatus = (newStatus ?? "").Trim().ToLower();
            if (newStatus != "approved" && newStatus != "rejected" && newStatus != "pending" && newStatus != "draft")
                newStatus = "draft";

            // TODO: Update policy status in database
            // var p = _context.Policies.FirstOrDefault(x => x.Id == id);
            // if (p != null) { p.Status = newStatus; p.LastUpdated = DateTime.Now.ToString("yyyy-MM-dd"); _context.SaveChanges(); }

            return RedirectToAction(nameof(PoliciesManagement), new { status, q });
        }

        // GET: /Home/LessonsLearned
        public IActionResult LessonsLearned()
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            var role = HttpContext.Session.GetString("Role") ?? "";
            bool canSubmit = role == "barangay_staff" || role == "barangay_secretary" || role == "barangay_admin" || role == "super_admin";

            // TODO: Fetch lessons from database
            // var lessons = _context.Lessons.OrderByDescending(l => l.Date).ToList();
            var lessons = new List<LessonRow>();

            // TODO: Fetch project types from database or use static list
            var projectTypes = new List<string>
            {
                "All Projects",
                "Health Program",
                "Finance Modernization",
                "Youth Development",
                "Disaster Risk Reduction",
                "Education",
                "Environment"
            };

            var vm = new LessonsLearnedViewModel
            {
                CanSubmit = canSubmit,
                Lessons = lessons,
                ProjectTypes = projectTypes
            };

            return View(vm);
        }

        // GET: /Home/BestPractices
        [HttpGet]
        public IActionResult BestPractices(string q = "", string category = "All Categories")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            q = (q ?? "").Trim().ToLower();
            category = string.IsNullOrWhiteSpace(category) ? "All Categories" : category.Trim();

            bool canManage = (HttpContext.Session.GetString("Role") ?? "") is "barangay_admin" or "super_admin" or "barangay_secretary";

            // TODO: Fetch practices from database
            // var allPractices = _context.Practices.ToList();
            var allPractices = new List<PracticeItem>();

            var list = allPractices;

            if (!string.IsNullOrWhiteSpace(q))
            {
                list = list.Where(p =>
                        (p.Title ?? "").ToLower().Contains(q) ||
                        (p.Description ?? "").ToLower().Contains(q))
                    .ToList();
            }

            if (category != "All Categories")
                list = list.Where(p => p.Category == category).ToList();

            // TODO: Fetch featured practice from database
            // var featured = allPractices.FirstOrDefault(p => p.Featured && p.Rating >= 4.8m);
            PracticeItem? featured = null;

            var vm = new BestPracticesViewModel
            {
                SearchQuery = q,
                SelectedCategory = category,
                CanManage = canManage,
                // TODO: Fetch categories from database
                Categories = new List<string> { "All Categories", "Health", "Education", "Governance", "Environment", "Safety", "Finance" },
                Practices = list,
                FeaturedPractice = featured
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreatePractice(string title, string category, string description, string barangay)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            var role = HttpContext.Session.GetString("Role") ?? "";
            if (role != "barangay_admin" && role != "super_admin" && role != "barangay_secretary")
                return RedirectToAction(nameof(BestPractices));

            title = (title ?? "").Trim();
            category = string.IsNullOrWhiteSpace(category) ? "Governance" : category.Trim();
            description = (description ?? "").Trim();
            barangay = string.IsNullOrWhiteSpace(barangay) ? (HttpContext.Session.GetString("Barangay") ?? "Unknown") : barangay.Trim();

            if (string.IsNullOrWhiteSpace(title))
                return RedirectToAction(nameof(BestPractices));

            // TODO: Add practice to database
            // var practice = new PracticeItem { ... };
            // _context.Practices.Add(practice);
            // _context.SaveChanges();

            return RedirectToAction(nameof(BestPractices));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditPractice(string id, string title, string category, string description, string barangay)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            var role = HttpContext.Session.GetString("Role") ?? "";
            if (role != "barangay_admin" && role != "super_admin" && role != "barangay_secretary")
                return RedirectToAction(nameof(BestPractices));

            // TODO: Fetch and update practice in database
            // var p = _context.Practices.FirstOrDefault(x => x.Id == id);
            // if (p != null) { ... _context.SaveChanges(); }

            return RedirectToAction(nameof(BestPractices));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePractice(string id)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            var role = HttpContext.Session.GetString("Role") ?? "";
            if (role != "barangay_admin" && role != "super_admin" && role != "barangay_secretary")
                return RedirectToAction(nameof(BestPractices));

            // TODO: Delete practice from database
            // var p = _context.Practices.FirstOrDefault(x => x.Id == id);
            // if (p != null) { _context.Practices.Remove(p); _context.SaveChanges(); }

            return RedirectToAction(nameof(BestPractices));
        }

        // GET: /Home/KnowledgeSharing
        [HttpGet]
        public IActionResult KnowledgeSharing()
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            var role = HttpContext.Session.GetString("Role") ?? "";

            bool canPost = role == "barangay_staff" || role == "barangay_secretary" || role == "barangay_admin" || role == "super_admin";
            bool canAnnounce = role == "barangay_admin" || role == "super_admin";

            // TODO: Fetch knowledge sharing data from database
            // var discussions = _context.KnowledgeDiscussions.OrderByDescending(x => x.Date).ToList();
            // var announcements = _context.KnowledgeAnnouncements.OrderByDescending(x => x.Pinned).ThenByDescending(x => x.Date).ToList();
            // var sharedDocs = _context.SharedDocuments.ToList();
            // var membersOnline = _context.Users.Count(u => u.IsOnline);

            var vm = new KnowledgeSharingViewModel
            {
                CanPost = canPost,
                CanAnnounce = canAnnounce,
                Discussions = new List<KnowledgeDiscussionItem>(),
                Announcements = new List<KnowledgeAnnouncementItem>(),
                SharedDocuments = new List<KnowledgeSharedDocItem>(),
                ActiveMembers = new List<string>(),
                MembersOnline = 0
            };

            return View(vm);
        }

        // POST: Quick Post
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult QuickPostKnowledge(string content)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            var role = HttpContext.Session.GetString("Role") ?? "";
            bool canPost = role == "barangay_staff" || role == "barangay_secretary" || role == "barangay_admin" || role == "super_admin";
            if (!canPost) return RedirectToAction(nameof(KnowledgeSharing));

            content = (content ?? "").Trim();
            if (string.IsNullOrWhiteSpace(content))
                return RedirectToAction(nameof(KnowledgeSharing));

            var name = HttpContext.Session.GetString("UserName") ?? "Unknown";
            var initials = string.Join("", name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(x => x[0])).ToUpper();
            if (initials.Length > 2) initials = initials.Substring(0, 2);
            if (string.IsNullOrWhiteSpace(initials)) initials = "U";

            // TODO: Add discussion post to database
            // var discussion = new KnowledgeDiscussionItem { ... };
            // _context.KnowledgeDiscussions.Add(discussion);
            // _context.SaveChanges();

            return RedirectToAction(nameof(KnowledgeSharing));
        }

        public IActionResult UserManagement()
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToAction(nameof(DashboardHome));

            // TODO: Fetch users from database
            // var users = _context.Users.ToList();
            var vm = new UserManagementViewModel
            {
                Users = new List<UserItem>()
            };

            return View(vm);
        }

        // GET: /Home/Announcements
        [HttpGet]
        public IActionResult Announcements(string filter = "all")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToAction(nameof(DashboardHome));

            filter = (filter ?? "all").Trim().ToLower();

            // TODO: Fetch announcements from database
            // var allAnnouncements = _context.Announcements.OrderByDescending(a => a.Pinned).ThenByDescending(a => a.Date).ToList();
            var allAnnouncements = new List<AnnouncementItem>();

            var list = allAnnouncements;

            if (filter != "all")
                list = list.Where(a => a.Status == filter).ToList();

            var vm = new AnnouncementsViewModel
            {
                Filter = filter,
                Announcements = list
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateAnnouncement(string title, string content, string priority, string status, string filter = "all")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToAction(nameof(DashboardHome));

            title = (title ?? "").Trim();
            content = (content ?? "").Trim();
            priority = string.IsNullOrWhiteSpace(priority) ? "medium" : priority.Trim().ToLower();
            status = string.IsNullOrWhiteSpace(status) ? "draft" : status.Trim().ToLower();

            if (string.IsNullOrWhiteSpace(title))
                return RedirectToAction(nameof(Announcements), new { filter });

            // TODO: Add announcement to database
            // var announcement = new AnnouncementItem { ... };
            // _context.Announcements.Add(announcement);
            // _context.SaveChanges();

            return RedirectToAction(nameof(Announcements), new { filter });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditAnnouncement(string id, string title, string content, string priority, string status, string filter = "all")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToAction(nameof(DashboardHome));

            // TODO: Fetch and update announcement in database
            // var a = _context.Announcements.FirstOrDefault(x => x.Id == id);
            // if (a != null) { ... _context.SaveChanges(); }

            return RedirectToAction(nameof(Announcements), new { filter });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteAnnouncement(string id, string filter = "all")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToAction(nameof(DashboardHome));

            // TODO: Delete announcement from database
            // var a = _context.Announcements.FirstOrDefault(x => x.Id == id);
            // if (a != null) { _context.Announcements.Remove(a); _context.SaveChanges(); }

            return RedirectToAction(nameof(Announcements), new { filter });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TogglePinAnnouncement(string id, string filter = "all")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToAction(nameof(DashboardHome));

            // TODO: Toggle pin status in database
            // var a = _context.Announcements.FirstOrDefault(x => x.Id == id);
            // if (a != null) { a.Pinned = !a.Pinned; _context.SaveChanges(); }

            return RedirectToAction(nameof(Announcements), new { filter });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult IncrementAnnouncementViews(string id, string filter = "all")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToAction(nameof(DashboardHome));

            // TODO: Increment views in database
            // var a = _context.Announcements.FirstOrDefault(x => x.Id == id);
            // if (a != null) { a.Views += 1; _context.SaveChanges(); }

            return RedirectToAction(nameof(Announcements), new { filter });
        }

        // GET: /Home/AuditLogs
        [HttpGet]
        public IActionResult AuditLogs(string q = "", string module = "all")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToAction(nameof(DashboardHome));

            q = (q ?? "").Trim().ToLower();
            module = (module ?? "all").Trim();

            // TODO: Fetch audit logs from database
            // var allLogs = _context.AuditLogs.OrderByDescending(l => l.Timestamp).ToList();
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
        public IActionResult DeleteLog(string id, string q = "", string module = "all")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToAction(nameof(DashboardHome));

            // TODO: Delete audit log from database
            // var log = _context.AuditLogs.FirstOrDefault(x => x.Id == id);
            // if (log != null) { _context.AuditLogs.Remove(log); _context.SaveChanges(); }

            return RedirectToAction(nameof(AuditLogs), new { q, module });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ClearAllLogs()
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToAction(nameof(DashboardHome));

            // TODO: Clear all audit logs from database
            // _context.AuditLogs.RemoveRange(_context.AuditLogs);
            // _context.SaveChanges();

            return RedirectToAction(nameof(AuditLogs));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ExportLogsCsv(string q = "", string module = "all")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToAction(nameof(DashboardHome));

            // apply same filter as GET
            q = (q ?? "").Trim().ToLower();
            module = (module ?? "all").Trim();

            // TODO: Fetch audit logs from database
            // var allLogs = _context.AuditLogs.ToList();
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
            if (!IsAdminRole()) return RedirectToAction(nameof(DashboardHome));

            tab = (tab ?? "general").Trim().ToLower();

            // TODO: Load user settings from database
            // var userSettings = _context.UserSettings.FirstOrDefault(s => s.UserId == currentUserId);
            var vm = new SettingsViewModel
            {
                Tab = tab,

                // Load from Session (empty defaults until database integration)
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

            // Optional: show messages from TempData
            vm.SuccessMessage = TempData["Success"] as string;
            vm.ErrorMessage = TempData["Error"] as string;

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveProfile(SettingsViewModel model)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToAction(nameof(DashboardHome));

            // Save to Session
            HttpContext.Session.SetString("Settings_FullName", (model.FullName ?? "").Trim());
            HttpContext.Session.SetString("Settings_Email", (model.Email ?? "").Trim());
            HttpContext.Session.SetString("Settings_Barangay", (model.Barangay ?? "").Trim());
            HttpContext.Session.SetString("Settings_Language", (model.Language ?? "en").Trim());

            // Also update display name/barangay shown in layout if you want
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
            if (!IsAdminRole()) return RedirectToAction(nameof(DashboardHome));

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
            if (!IsAdminRole()) return RedirectToAction(nameof(DashboardHome));

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

            // TODO: Verify current password and update in database
            // var user = _context.Users.FirstOrDefault(u => u.Id == currentUserId);
            // if (user == null || !VerifyPassword(currentPassword, user.PasswordHash))
            // {
            //     TempData["Error"] = "Current password is incorrect.";
            //     return RedirectToAction(nameof(Settings), new { tab = "security" });
            // }
            // user.PasswordHash = HashPassword(newPassword);
            // _context.SaveChanges();

            TempData["Success"] = "Password updated. Your password has been changed.";
            return RedirectToAction(nameof(Settings), new { tab = "security" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleTwoFa(bool twoFaEnabled)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToAction(nameof(DashboardHome));

            // TODO: Update 2FA setting in database
            // var user = _context.Users.FirstOrDefault(u => u.Id == currentUserId);
            // if (user != null) { user.TwoFaEnabled = twoFaEnabled; _context.SaveChanges(); }

            HttpContext.Session.SetString("Settings_TwoFa", twoFaEnabled ? "true" : "false");
            TempData["Success"] = twoFaEnabled ? "2FA Enabled." : "2FA Disabled.";
            return RedirectToAction(nameof(Settings), new { tab = "security" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveSystem(SettingsViewModel model)
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToAction(nameof(DashboardHome));

            // TODO: Save system settings to database
            // var settings = _context.SystemSettings.FirstOrDefault() ?? new SystemSettings();
            // settings.MaintenanceMode = model.MaintenanceMode;
            // settings.SessionTimeout = model.SessionTimeout;
            // settings.DocFormat = model.DocFormat;
            // _context.SaveChanges();

            HttpContext.Session.SetString("Settings_Maintenance", model.MaintenanceMode ? "true" : "false");
            HttpContext.Session.SetString("Settings_SessionTimeout", model.SessionTimeout ?? "30");
            HttpContext.Session.SetString("Settings_DocFormat", model.DocFormat ?? "pdf");

            TempData["Success"] = "System settings saved. System preferences have been updated.";
            return RedirectToAction(nameof(Settings), new { tab = "system" });
        }

        public IActionResult PasswordRequests()
        {
            // TODO: Fetch password reset requests from database
            // var requests = _context.PasswordResetRequests.ToList();
            return View();
        }

        // GET: /Home/ForgotPassword
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            // This page is public (no login required)
            return View(new ForgotPasswordViewModel());
        }

        // POST: /Home/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ForgotPassword(ForgotPasswordViewModel model)
        {
            // Public page validation
            if (!ModelState.IsValid)
            {
                model.Submitted = false;
                return View(model);
            }

            // TODO: Create password reset request in database and send email
            // var user = _context.Users.FirstOrDefault(u => u.Email == model.Email);
            // if (user != null)
            // {
            //     var resetToken = GenerateResetToken();
            //     var request = new PasswordResetRequest { UserId = user.Id, Token = resetToken, ExpiresAt = DateTime.UtcNow.AddHours(24) };
            //     _context.PasswordResetRequests.Add(request);
            //     _context.SaveChanges();
            //     SendResetEmail(user.Email, resetToken);
            // }

            model.Submitted = true;
            model.SuccessMessage = "If your email is registered, you will receive reset instructions.";

            return View(model);
        }


        // GET: /Home/Logout
        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(Index));
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
