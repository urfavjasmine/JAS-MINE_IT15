using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using JAS_MINE_IT15.Models;
using System.Collections.Generic;

namespace JAS_MINE_IT15.Controllers
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
        private bool IsLoggedIn()
        {
            return !string.IsNullOrEmpty(HttpContext.Session.GetString("UserName"));
        }

        public IActionResult DashboardHome()
        {
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
                return RedirectToAction(nameof(Login));

            return View();
        }

        private bool NotLoggedIn() => string.IsNullOrEmpty(HttpContext.Session.GetString("UserName"));

        public IActionResult KnowledgeRepository()
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            return View();
        }

        public IActionResult PoliciesManagement()
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            return View();
        }

        public IActionResult LessonsLearned()
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            return View();
        }

        public IActionResult BestPractices()
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            return View();
        }

        public IActionResult KnowledgeSharing()
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            return View();
        }

        // GET: /Home/Login
        [HttpGet]
        public IActionResult Login()
        {
            var existing = HttpContext.Session.GetString("UserName");
            if (!string.IsNullOrEmpty(existing))
                return RedirectToAction(nameof(DashboardHome));

            return View(new LoginViewModel());
        }

        // POST: /Home/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (string.IsNullOrWhiteSpace(model.Role))
            {
                model.ErrorMessage = "Please select a role.";
                return View(model);
            }

            model.Email = (model.Email ?? "").Trim();
            model.Password = (model.Password ?? "").Trim();
            model.Role = (model.Role ?? "").Trim();

            if (!DefaultAccounts.TryGetValue(model.Role, out var acc))
            {
                model.ErrorMessage = "Invalid role selected.";
                return View(model);
            }

            if (string.Equals(model.Email, acc.Email, StringComparison.OrdinalIgnoreCase)
                && model.Password == acc.Password)
            {
                HttpContext.Session.SetString("UserName", acc.Name);
                HttpContext.Session.SetString("Role", model.Role);
                HttpContext.Session.SetString("RoleLabel", GetRoleLabel(model.Role));
                HttpContext.Session.SetString("Barangay", "Barangay San Antonio");

                // ✅ FORCE SESSION TO COMMIT
                await HttpContext.Session.CommitAsync();

                return RedirectToAction(nameof(DashboardHome));
            }

            model.ErrorMessage = "Invalid credentials for the selected role.";
            return View(model);
        }


        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ForgotPassword(string email)
        {
            ViewBag.Message = "Password reset link sent to your email.";
            return View();
        }

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
