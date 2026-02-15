using JAS_MINE_IT15.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JAS_MINE_IT15.Controllers99
{
    public class HomeController : Controller
    {
        // TEMP DEFAULT ACCOUNTS (NO DATABASE)
        private static readonly Dictionary<string, (string Email, string Password, string Name)> DefaultAccounts
            = new()
            {
                ["super_admin"] = ("superadmin@gmail.com", "1234", "Jasmine T. Elederos"),
                ["barangay_admin"] = ("admin@gmail.com", "1234", "Barangay Admin"),
                ["barangay_secretary"] = ("secretary@gmail.com", "1234", "Barangay Secretary"),
                ["barangay_staff"] = ("staff@gmail.com", "1234", "Barangay Staff"),
                ["council_member"] = ("council@gmail.com", "1234", "Council Member"),
            };

        // ✅ KNOWLEDGE REPOSITORY (TEMP DATA IN MEMORY)
        private static readonly List<RepoDocument> _repoDocs = new()
            {
                new RepoDocument { Id="1", Title="Resolution No. 2024-001", Category="Resolutions", TagsCsv="governance, budget", UploadedBy="Maria Santos", Date="2024-02-01", Status="approved", Version="1.2", Description="Annual budget resolution for fiscal year 2024." },
                new RepoDocument { Id="2", Title="Ordinance on Waste Management", Category="Ordinances", TagsCsv="environment, health", UploadedBy="Juan dela Cruz", Date="2024-01-28", Status="approved", Version="2.0", Description="Comprehensive waste management ordinance." },
                new RepoDocument { Id="3", Title="Memorandum Circular 2024-005", Category="Memorandums", TagsCsv="operations, staff", UploadedBy="Ana Reyes", Date="2024-01-25", Status="pending", Version="1.0", Description="Staff operational guidelines update." },
                new RepoDocument { Id="4", Title="Budget Proposal FY 2024", Category="Policies", TagsCsv="budget, finance", UploadedBy="Pedro Garcia", Date="2024-01-20", Status="approved", Version="3.1", Description="Proposed budget for FY 2024." },
                new RepoDocument { Id="5", Title="Community Health Program Guidelines", Category="Policies", TagsCsv="health, community", UploadedBy="Maria Santos", Date="2024-01-15", Status="draft", Version="1.0", Description="Guidelines for community health programs." },
            };

        // ✅ POLICIES (TEMP DATA IN MEMORY)
        private static readonly List<PolicyItem> _policies = new()
            {
                new PolicyItem { Id="1", Title="Waste Management Policy",
                    Description="Guidelines for proper waste segregation and disposal in the barangay.",
                    Status="approved", LastUpdated="2024-02-01", Author="Maria Santos", Version="2.1" },

                new PolicyItem { Id="2", Title="Community Health Guidelines",
                    Description="Health protocols and emergency response procedures for residents.",
                    Status="pending", LastUpdated="2024-01-28", Author="Juan dela Cruz", Version="1.0" },

                new PolicyItem { Id="3", Title="Budget Allocation Policy",
                    Description="Standard operating procedures for budget allocation and disbursement.",
                    Status="draft", LastUpdated="2024-01-25", Author="Ana Reyes", Version="0.9" },

                new PolicyItem { Id="4", Title="Business Permit Guidelines",
                    Description="Requirements and procedures for obtaining business permits.",
                    Status="approved", LastUpdated="2024-01-20", Author="Pedro Garcia", Version="3.0" },

                new PolicyItem { Id="5", Title="Peace and Order Procedures",
                    Description="Protocols for maintaining peace and order in the community.",
                    Status="approved", LastUpdated="2024-01-15", Author="Maria Santos", Version="1.5" },
            };

        // ✅ BEST PRACTICES (TEMP DATA IN MEMORY)
        private static readonly List<PracticeItem> _practices = new()
            {
                new PracticeItem { Id="1", Title="Community-Based Health Monitoring System", Category="Health",
                    Description="A volunteer-driven health monitoring program that tracks vital signs of elderly residents through regular home visits and mobile reporting.",
                    Barangay="Brgy. San Antonio", DateAdded="2024-01-15", Rating=4.8m, Implementations=12, Featured=true },

                new PracticeItem { Id="2", Title="Youth Education Scholarship Program", Category="Education",
                    Description="Sustainable scholarship fund management through community contributions and transparent selection process.",
                    Barangay="Brgy. Malabon", DateAdded="2024-01-10", Rating=4.6m, Implementations=8, Featured=true },

                new PracticeItem { Id="3", Title="Digital Barangay Services Portal", Category="Governance",
                    Description="Online portal for requesting certificates, permits, and tracking service requests with SMS notifications.",
                    Barangay="Brgy. Poblacion", DateAdded="2024-01-05", Rating=4.9m, Implementations=25, Featured=false },

                new PracticeItem { Id="4", Title="Zero Waste Community Program", Category="Environment",
                    Description="Comprehensive waste reduction initiative including composting centers, recycling hubs, and incentive programs.",
                    Barangay="Brgy. Pinyahan", DateAdded="2023-12-20", Rating=4.7m, Implementations=15, Featured=true },

                new PracticeItem { Id="5", Title="Barangay Emergency Response System", Category="Safety",
                    Description="Integrated emergency response system with trained volunteer responders, communication protocols, and equipment management.",
                    Barangay="Brgy. Bagong Silang", DateAdded="2023-12-15", Rating=4.5m, Implementations=10, Featured=false },
            };

        // ✅ ANNOUNCEMENTS (TEMP DATA IN MEMORY)
        private static readonly List<AnnouncementItem> _announcements = new()
            {
                new AnnouncementItem { Id="1", Title="System Maintenance Notice",
                    Content="The system will undergo scheduled maintenance on February 15, 2026 from 10:00 PM to 2:00 AM.",
                    Priority="high", Status="published", Date="2026-02-08", Author="Maria Santos", Views=45, Pinned=true },

                new AnnouncementItem { Id="2", Title="New Policy Upload Guidelines",
                    Content="All barangay secretaries are reminded to follow the updated document upload guidelines effective immediately.",
                    Priority="medium", Status="published", Date="2026-02-07", Author="Juan Dela Cruz", Views=32, Pinned=false },

                new AnnouncementItem { Id="3", Title="Training Session: Knowledge Management",
                    Content="A training session on using the Knowledge Repository module will be held on February 20, 2026.",
                    Priority="low", Status="draft", Date="2026-02-06", Author="Maria Santos", Views=0, Pinned=false },

                new AnnouncementItem { Id="4", Title="Annual Barangay Reports Due",
                    Content="All barangays are reminded to submit their annual reports by March 31, 2026.",
                    Priority="high", Status="published", Date="2026-02-05", Author="Maria Santos", Views=67, Pinned=true },
            };

        // ✅ AUDIT LOGS (TEMP DATA IN MEMORY)
        private static readonly List<LogItem> _auditLogs = new()
            {
                new LogItem { Id="1", Timestamp="2026-02-09 14:32:10", User="Maria Santos", Action="Approved", Module="Policies", Target="Resolution No. 2026-001", Ip="192.168.1.10" },
                new LogItem { Id="2", Timestamp="2026-02-09 13:15:44", User="Juan Dela Cruz", Action="Uploaded", Module="Repository", Target="Annual Budget Report 2026.pdf", Ip="192.168.1.11" },
                new LogItem { Id="3", Timestamp="2026-02-09 11:45:22", User="Ana Reyes", Action="Submitted", Module="Lessons Learned", Target="Community Health Program Review", Ip="192.168.1.12" },
                new LogItem { Id="4", Timestamp="2026-02-08 16:20:05", User="Maria Santos", Action="Rejected", Module="Best Practices", Target="Waste Management Initiative", Ip="192.168.1.10" },
                new LogItem { Id="5", Timestamp="2026-02-08 10:05:30", User="Rosa Mendoza", Action="Posted", Module="Knowledge Sharing", Target="Discussion: Budget Allocation Tips", Ip="192.168.1.15" },
                new LogItem { Id="6", Timestamp="2026-02-07 09:12:18", User="Maria Santos", Action="Created", Module="User Management", Target="New user: Pedro Garcia", Ip="192.168.1.10" },
                new LogItem { Id="7", Timestamp="2026-02-07 08:30:00", User="Juan Dela Cruz", Action="Updated", Module="Policies", Target="Ordinance No. 2025-045", Ip="192.168.1.11" },
                new LogItem { Id="8", Timestamp="2026-02-06 15:45:12", User="Ana Reyes", Action="Deleted", Module="Repository", Target="Draft memo (duplicate)", Ip="192.168.1.12" },
            };

        // ✅ KNOWLEDGE SHARING (TEMP DATA IN MEMORY)
        private static readonly List<KnowledgeDiscussionItem> _ksDiscussions = new()
        {
            new KnowledgeDiscussionItem { Id="1", Title="Best approach for community health monitoring?",
                Content="Looking for suggestions on implementing a sustainable health monitoring program for our senior citizens...",
                Author="Maria Santos", Avatar="MS", Date="2024-02-02", Category="Health", Replies=8, Likes=15 },

            new KnowledgeDiscussionItem { Id="2", Title="Waste segregation enforcement strategies",
                Content="Our barangay is struggling with waste segregation compliance. What strategies have worked for others?",
                Author="Juan dela Cruz", Avatar="JD", Date="2024-02-01", Category="Environment", Replies=12, Likes=28 },

            new KnowledgeDiscussionItem { Id="3", Title="Youth engagement programs success stories",
                Content="Share your successful youth engagement programs! We want to replicate what works.",
                Author="Ana Reyes", Avatar="AR", Date="2024-01-30", Category="Youth", Replies=6, Likes=22 },
        };

        private static readonly List<KnowledgeAnnouncementItem> _ksAnnouncements = new()
        {
            new KnowledgeAnnouncementItem { Id="1", Title="New Document Submission Guidelines",
                Content="All document submissions must now include the new metadata form. Please review the updated guidelines in the Knowledge Repository.",
                Author="Barangay Administrator", Date="2024-02-01", Pinned=true, Likes=45, Comments=12 },

            new KnowledgeAnnouncementItem { Id="2", Title="System Maintenance Schedule",
                Content="JAS-MINE will undergo scheduled maintenance on February 15, 2024 from 10:00 PM to 12:00 AM.",
                Author="System Admin", Date="2024-01-30", Pinned=true, Likes=23, Comments=5 },
        };

        private static readonly List<KnowledgeSharedDocItem> _ksSharedDocs = new()
        {
            new KnowledgeSharedDocItem { Id="1", Title="Community Health Survey Template", SharedBy="Maria Santos", Date="2024-02-01", Downloads=34 },
            new KnowledgeSharedDocItem { Id="2", Title="Budget Planning Worksheet", SharedBy="Pedro Garcia", Date="2024-01-28", Downloads=56 },
            new KnowledgeSharedDocItem { Id="3", Title="Event Planning Checklist", SharedBy="Ana Reyes", Date="2024-01-25", Downloads=42 },
        };

        private bool IsLoggedIn() =>
            !string.IsNullOrEmpty(HttpContext.Session.GetString("UserName"));

        private bool IsAdminRole()
        {
            var role = HttpContext.Session.GetString("Role");
            return role == "super_admin" || role == "barangay_admin";
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

        // ✅ UPDATED POST: /Home/Login (NO ROLE DROPDOWN / NO CHOOSE ROLE)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model)
        {
            // ✅ show model validation errors
            if (!ModelState.IsValid)
            {
                model.ErrorMessage = "Please fill up all required fields.";
                return View(model);
            }

            // sanitize inputs (role removed)
            model.Email = (model.Email ?? "").Trim();
            model.Password = (model.Password ?? "").Trim();

            // Find matching account by email + password (role is determined automatically)
            var match = DefaultAccounts.FirstOrDefault(kvp =>
                string.Equals(kvp.Value.Email, model.Email, StringComparison.OrdinalIgnoreCase) &&
                kvp.Value.Password == model.Password
            );

            // If no match
            if (string.IsNullOrEmpty(match.Key))
            {
                model.ErrorMessage = "Invalid email/password.";
                return View(model);
            }

            var role = match.Key;
            var acc = match.Value;

            // set session (this is what Dashboard checks)
            HttpContext.Session.SetString("UserName", acc.Name);
            HttpContext.Session.SetString("Role", role);
            HttpContext.Session.SetString("RoleLabel", GetRoleLabel(role));
            HttpContext.Session.SetString("Barangay", "Barangay San Antonio");

            // redirect to dashboard
            return RedirectToAction(nameof(DashboardHome));
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

            // example DB counts (replace with your DB context)
            vm.TotalDocuments = 10;
            vm.ActivePolicies = 5;
            vm.LessonsLearned = 3;
            vm.BestPractices = 2;

            vm.ModuleUsage = new List<ModuleUsageRow>
            {
                new() { Name = "Repository", Value = 40 },
                new() { Name = "Policies", Value = 25 },
                new() { Name = "Lessons", Value = 20 },
                new() { Name = "Practices", Value = 15 }
            };

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

            var list = _repoDocs.OrderByDescending(d => d.Date).ToList();

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

            _repoDocs.Insert(0, new RepoDocument
            {
                Id = DateTime.Now.Ticks.ToString(),
                Title = title,
                Category = string.IsNullOrWhiteSpace(category) ? "Policies" : category.Trim(),
                TagsCsv = (tags ?? "").Trim(),
                Description = (description ?? "").Trim(),
                UploadedBy = uploader,
                Date = DateTime.Now.ToString("yyyy-MM-dd"),
                Status = "draft",
                Version = "1.0"
            });

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

            var doc = _repoDocs.FirstOrDefault(d => d.Id == id);
            if (doc != null)
            {
                if (!string.IsNullOrWhiteSpace(title)) doc.Title = title.Trim();
                if (!string.IsNullOrWhiteSpace(category)) doc.Category = category.Trim();
                doc.TagsCsv = (tags ?? "").Trim();
                doc.Description = (description ?? "").Trim();

                // bump version a bit (simple)
                doc.Version = doc.Version == "1.0" ? "1.1" : doc.Version;
            }

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

            _repoDocs.RemoveAll(d => d.Id == id);
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

            var doc = _repoDocs.FirstOrDefault(d => d.Id == id);
            if (doc != null) doc.Status = "approved";

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

            var doc = _repoDocs.FirstOrDefault(d => d.Id == id);
            if (doc != null) doc.Status = "rejected";

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

            var list = _policies
                .OrderByDescending(x => x.LastUpdated)
                .ToList();

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

                CountAll = _policies.Count,
                CountApproved = _policies.Count(x => x.Status == "approved"),
                CountPending = _policies.Count(x => x.Status == "pending"),
                CountDraft = _policies.Count(x => x.Status == "draft"),

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

            _policies.Insert(0, new PolicyItem
            {
                Id = DateTime.Now.Ticks.ToString(),
                Title = title,
                Description = description,
                Status = "draft",
                LastUpdated = DateTime.Now.ToString("yyyy-MM-dd"),
                Author = HttpContext.Session.GetString("UserName") ?? "Unknown",
                Version = "1.0"
            });

            return RedirectToAction(nameof(PoliciesManagement), new { status, q });
        }

        // This is used for Edit action (just updates title/description, not status)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditPolicy(string id, string title, string description, string status = "all", string q = "")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            var p = _policies.FirstOrDefault(x => x.Id == id);
            if (p != null)
            {
                p.Title = string.IsNullOrWhiteSpace(title) ? p.Title : title.Trim();
                p.Description = (description ?? "").Trim();
                p.LastUpdated = DateTime.Now.ToString("yyyy-MM-dd");
            }

            return RedirectToAction(nameof(PoliciesManagement), new { status, q });
        }

        // This is used for Delete action (removes from list)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePolicy(string id, string status = "all", string q = "")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            _policies.RemoveAll(x => x.Id == id);

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

            var p = _policies.FirstOrDefault(x => x.Id == id);
            if (p != null)
            {
                p.Status = newStatus;
                p.LastUpdated = DateTime.Now.ToString("yyyy-MM-dd");
            }

            return RedirectToAction(nameof(PoliciesManagement), new { status, q });
        }

        // GET: /Home/LessonsLearned
        public IActionResult LessonsLearned()
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            return View();
        }

        // GET: /Home/BestPractices
        [HttpGet]
        public IActionResult BestPractices(string q = "", string category = "All Categories")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            q = (q ?? "").Trim().ToLower();
            category = string.IsNullOrWhiteSpace(category) ? "All Categories" : category.Trim();

            bool canManage = (HttpContext.Session.GetString("Role") ?? "") is "barangay_admin" or "super_admin" or "barangay_secretary";

            var list = _practices.ToList();

            if (!string.IsNullOrWhiteSpace(q))
            {
                list = list.Where(p =>
                        (p.Title ?? "").ToLower().Contains(q) ||
                        (p.Description ?? "").ToLower().Contains(q))
                    .ToList();
            }

            if (category != "All Categories")
                list = list.Where(p => p.Category == category).ToList();

            var featured = _practices.FirstOrDefault(p => p.Featured && p.Rating >= 4.8m);

            var vm = new BestPracticesViewModel
            {
                SearchQuery = q,
                SelectedCategory = category,
                CanManage = canManage,
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

            _practices.Insert(0, new PracticeItem
            {
                Id = DateTime.Now.Ticks.ToString(),
                Title = title,
                Category = category,
                Description = description,
                Barangay = barangay,
                DateAdded = DateTime.Now.ToString("yyyy-MM-dd"),
                Rating = 0m,
                Implementations = 0,
                Featured = false
            });

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

            var p = _practices.FirstOrDefault(x => x.Id == id);
            if (p != null)
            {
                p.Title = string.IsNullOrWhiteSpace(title) ? p.Title : title.Trim();
                p.Category = string.IsNullOrWhiteSpace(category) ? p.Category : category.Trim();
                p.Description = (description ?? "").Trim();
                p.Barangay = string.IsNullOrWhiteSpace(barangay) ? p.Barangay : barangay.Trim();
            }

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

            _practices.RemoveAll(x => x.Id == id);
            return RedirectToAction(nameof(BestPractices));
        }

        // ✅ GET: /Home/KnowledgeSharing
        [HttpGet]
        public IActionResult KnowledgeSharing()
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));

            var role = HttpContext.Session.GetString("Role") ?? "";

            bool canPost = role == "barangay_staff" || role == "barangay_secretary" || role == "barangay_admin" || role == "super_admin";
            bool canAnnounce = role == "barangay_admin" || role == "super_admin";

            var vm = new KnowledgeSharingViewModel
            {
                CanPost = canPost,
                CanAnnounce = canAnnounce,
                Discussions = _ksDiscussions.OrderByDescending(x => x.Date).ToList(),
                Announcements = _ksAnnouncements.OrderByDescending(x => x.Pinned).ThenByDescending(x => x.Date).ToList(),
                SharedDocuments = _ksSharedDocs.ToList(),
                ActiveMembers = new List<string> { "MS", "JD", "AR", "PG", "LC" },
                MembersOnline = 17
            };

            return View(vm);
        }

        // ✅ POST: Quick Post
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

            _ksDiscussions.Insert(0, new KnowledgeDiscussionItem
            {
                Id = DateTime.Now.Ticks.ToString(),
                Title = content.Length > 60 ? content.Substring(0, 60) + "..." : content,
                Content = content,
                Author = name,
                Avatar = initials,
                Date = DateTime.Now.ToString("yyyy-MM-dd"),
                Category = "General",
                Replies = 0,
                Likes = 0
            });

            return RedirectToAction(nameof(KnowledgeSharing));
        }

        public IActionResult UserManagement()
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToAction(nameof(DashboardHome));

            var vm = new UserManagementViewModel
            {
                Users = new List<UserItem>
                {
                    new UserItem { Id="1", Name="Maria Santos", Email="maria@brgy.gov.ph", Role=UserRole.barangay_admin, Status="active", Barangay="Barangay 1" },
                    new UserItem { Id="2", Name="Juan Dela Cruz", Email="juan@brgy.gov.ph", Role=UserRole.barangay_secretary, Status="active", Barangay="Barangay 1" },
                    new UserItem { Id="3", Name="Ana Reyes", Email="ana@brgy.gov.ph", Role=UserRole.barangay_staff, Status="active", Barangay="Barangay 1" },
                    new UserItem { Id="4", Name="Pedro Garcia", Email="pedro@brgy.gov.ph", Role=UserRole.council_member, Status="inactive", Barangay="Barangay 1" },
                    new UserItem { Id="5", Name="Rosa Mendoza", Email="rosa@brgy.gov.ph", Role=UserRole.barangay_staff, Status="active", Barangay="Barangay 2" },
                }
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

            var list = _announcements
                .OrderByDescending(a => a.Pinned)
                .ThenByDescending(a => a.Date)
                .ToList();

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

            _announcements.Insert(0, new AnnouncementItem
            {
                Id = DateTime.Now.Ticks.ToString(),
                Title = title,
                Content = content,
                Priority = priority,
                Status = status,
                Date = DateTime.Now.ToString("yyyy-MM-dd"),
                Author = HttpContext.Session.GetString("UserName") ?? "Admin",
                Views = 0,
                Pinned = false
            });

            return RedirectToAction(nameof(Announcements), new { filter });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditAnnouncement(string id, string title, string content, string priority, string status, string filter = "all")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToAction(nameof(DashboardHome));

            var a = _announcements.FirstOrDefault(x => x.Id == id);
            if (a != null)
            {
                a.Title = (title ?? a.Title).Trim();
                a.Content = (content ?? a.Content).Trim();
                a.Priority = string.IsNullOrWhiteSpace(priority) ? a.Priority : priority.Trim().ToLower();
                a.Status = string.IsNullOrWhiteSpace(status) ? a.Status : status.Trim().ToLower();
            }

            return RedirectToAction(nameof(Announcements), new { filter });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteAnnouncement(string id, string filter = "all")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToAction(nameof(DashboardHome));

            _announcements.RemoveAll(x => x.Id == id);
            return RedirectToAction(nameof(Announcements), new { filter });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TogglePinAnnouncement(string id, string filter = "all")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToAction(nameof(DashboardHome));

            var a = _announcements.FirstOrDefault(x => x.Id == id);
            if (a != null) a.Pinned = !a.Pinned;

            return RedirectToAction(nameof(Announcements), new { filter });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult IncrementAnnouncementViews(string id, string filter = "all")
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToAction(nameof(DashboardHome));

            var a = _announcements.FirstOrDefault(x => x.Id == id);
            if (a != null) a.Views += 1;

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

            var list = _auditLogs
                .OrderByDescending(l => l.Timestamp)
                .ToList();

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

            _auditLogs.RemoveAll(x => x.Id == id);
            return RedirectToAction(nameof(AuditLogs), new { q, module });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ClearAllLogs()
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!IsAdminRole()) return RedirectToAction(nameof(DashboardHome));

            _auditLogs.Clear();
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

            var list = _auditLogs.AsEnumerable();

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

            var vm = new SettingsViewModel
            {
                Tab = tab,

                // Load from Session (fallbacks)
                FullName = HttpContext.Session.GetString("Settings_FullName") ?? (HttpContext.Session.GetString("UserName") ?? "Jasminetampus30"),
                Email = HttpContext.Session.GetString("Settings_Email") ?? "jasminetampus30@gmail.com",
                Barangay = HttpContext.Session.GetString("Settings_Barangay") ?? (HttpContext.Session.GetString("Barangay") ?? "Barangay San Antonio"),
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

            // TEMP only: validation like React (no real password change without DB)
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
            if (!IsAdminRole()) return RedirectToAction(nameof(DashboardHome));

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

            HttpContext.Session.SetString("Settings_Maintenance", model.MaintenanceMode ? "true" : "false");
            HttpContext.Session.SetString("Settings_SessionTimeout", model.SessionTimeout ?? "30");
            HttpContext.Session.SetString("Settings_DocFormat", model.DocFormat ?? "pdf");

            TempData["Success"] = "System settings saved. System preferences have been updated.";
            return RedirectToAction(nameof(Settings), new { tab = "system" });
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

            // TEMP: simulate sending reset link (no database / no email service yet)
            model.Submitted = true;
            model.SuccessMessage = "If your email is registered, you will receive reset instructions.";

            return View(model);
        }


        // GET: /Home/Logout
        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(Login));
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
