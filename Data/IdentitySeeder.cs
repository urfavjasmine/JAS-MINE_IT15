using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using JAS_MINE_IT15.Models.Entities;

namespace JAS_MINE_IT15.Data
{
    public static class IdentitySeeder
    {
        public static async Task SeedRoles(IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            string[] roles =
            {
                "super_admin",
                "barangay_admin",
                "barangay_secretary",
                "barangay_staff",
                "council_member"
            };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        public static async Task SeedSuperAdmin(IServiceProvider services)
        {
            var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

            string email = "admin@jasmine.gov.ph";
            string password = "JasMine@1234";

            var user = await userManager.FindByEmailAsync(email);

            if (user == null)
            {
                user = new IdentityUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(user, password);
                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "super_admin");
                    Console.WriteLine($"[Seeder] Created super_admin: {email}");
                }
                else
                {
                    Console.WriteLine($"[Seeder] FAILED to create {email}: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
                }
            }
            else
            {
                // Ensure role is assigned (do NOT reset password on every startup)
                if (!await userManager.IsInRoleAsync(user, "super_admin"))
                    await userManager.AddToRoleAsync(user, "super_admin");
                Console.WriteLine($"[Seeder] super_admin already exists: {email}");
            }
        }
        public static async Task SeedDefaultUsers(IServiceProvider services)
        {
            var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

            var defaults = new[]
            {
        new { Email="brgyadmin@brgy.gov.ph", Password="BrgyAdmin@1234", Role="barangay_admin", Name="Barangay Administrator" },
        new { Email="secretary@brgy.gov.ph", Password="Secretary@1234", Role="barangay_secretary", Name="Barangay Secretary" },
        new { Email="staff@brgy.gov.ph", Password="Staff@1234", Role="barangay_staff", Name="Barangay Staff" },
        new { Email="council@brgy.gov.ph", Password="Council@1234", Role="council_member", Name="Barangay Council Member" },
     };

            foreach (var d in defaults)
            {
                var user = await userManager.FindByEmailAsync(d.Email);

                if (user == null)
                {
                    user = new IdentityUser
                    {
                        UserName = d.Email,
                        Email = d.Email,
                        EmailConfirmed = true
                    };

                    await userManager.CreateAsync(user, d.Password);
                }
                // Do NOT reset password on every startup — only seed on first creation

                if (!await userManager.IsInRoleAsync(user, d.Role))
                    await userManager.AddToRoleAsync(user, d.Role);
            }
        }

        /// <summary>
        /// Seeds a test barangay and links all seeded default users to it (BusinessUsers table).
        /// Ensures test accounts have a valid BarangayId so the full subscription flow works.
        /// </summary>
        public static async Task SeedTestBarangayAndLinkUsers(ApplicationDbContext context)
        {
            // Ensure at least one test barangay exists
            var testBarangay = await context.Barangays
                .FirstOrDefaultAsync(b => b.Name == "Barangay San Isidro");

            if (testBarangay == null)
            {
                testBarangay = new Barangay
                {
                    Name = "Barangay San Isidro",
                    Code = "BSI-001",
                    Municipality = "Sample Municipality",
                    Province = "Sample Province",
                    Region = "Region IV-A",
                    ContactEmail = "sanisidro@brgy.gov.ph",
                    ContactPhone = "09171234567",
                    Address = "123 Main St, Barangay San Isidro",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };
                context.Barangays.Add(testBarangay);
                await context.SaveChangesAsync();
                Console.WriteLine($"[Seeder] Created test barangay: {testBarangay.Name} (Id: {testBarangay.Id})");
            }

            // Link default test users to the test barangay
            var testEmails = new[]
            {
                "brgyadmin@brgy.gov.ph",
                "secretary@brgy.gov.ph",
                "staff@brgy.gov.ph",
                "council@brgy.gov.ph"
            };

            foreach (var email in testEmails)
            {
                var businessUser = await context.BusinessUsers
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower() && u.IsActive);

                if (businessUser != null && businessUser.BarangayId == null)
                {
                    businessUser.BarangayId = testBarangay.Id;
                    businessUser.BarangayName = testBarangay.Name;
                    businessUser.UpdatedAt = DateTime.Now;
                    Console.WriteLine($"[Seeder] Linked {email} to {testBarangay.Name} (BarangayId: {testBarangay.Id})");
                }
            }

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Seeds 3 subscription plans: Basic, Professional, and Enterprise (monthly).
        /// Deactivates any old Standard/Premium plans. Skips if plans already exist.
        /// </summary>
        public static async Task SeedSubscriptionPlans(ApplicationDbContext context)
        {
            // Hard-delete old plans that are no longer offered
            var unwantedNames = new[] { "Standard", "Premium", "Enterprise" };
            var oldPlans = await context.SubscriptionPlans
                .Where(p => unwantedNames.Contains(p.Name))
                .ToListAsync();

            foreach (var old in oldPlans)
            {
                var hasSubscriptions = await context.BarangaySubscriptions
                    .AnyAsync(s => s.PlanId == old.Id);

                if (!hasSubscriptions)
                {
                    context.SubscriptionPlans.Remove(old);
                    Console.WriteLine($"[Seeder] Deleted old plan: {old.Name}");
                }
                else if (old.IsActive)
                {
                    old.IsActive = false;
                    old.UpdatedAt = DateTime.Now;
                    Console.WriteLine($"[Seeder] Deactivated old plan: {old.Name} (has subscriptions)");
                }
            }

            var planDefs = new[]
            {
                new
                {
                    Name        = "Basic Plan",
                    Description = "Essential tools for small barangays getting started.",
                    Price       = 500.00m,
                    Duration    = 1,
                    UserLimit   = 5,
                    Features    = string.Join(";",
                        "Knowledge Repository Module",
                        "Policy Management Module",
                        "Email Support")
                },
                new
                {
                    Name        = "Standard Plan",
                    Description = "Everything you need to manage your barangay records efficiently.",
                    Price       = 5000.00m,
                    Duration    = 12,
                    UserLimit   = 20,
                    Features    = string.Join(";",
                        "All Basic features",
                        "Lessons Learned Module",
                        "Best Practices Module",
                        "Priority Support")
                },
                new
                {
                    Name        = "Enterprise Plan",
                    Description = "Complete access with advanced tools and detailed tracking.",
                    Price       = 8000.00m,
                    Duration    = 12,
                    UserLimit   = 999,
                    Features    = string.Join(";",
                        "Full ERP access",
                        "Knowledge Sharing Portal",
                        "Audit Logs",
                        "Dedicated Support")
                }
            };

            foreach (var def in planDefs)
            {
                var existing = await context.SubscriptionPlans
                    .FirstOrDefaultAsync(p => p.Name == def.Name);

                if (existing == null)
                {
                    context.SubscriptionPlans.Add(new SubscriptionPlan
                    {
                        Name           = def.Name,
                        Description    = def.Description,
                        Price          = def.Price,
                        DurationMonths = def.Duration,
                        UserLimit      = def.UserLimit,
                        Features       = def.Features,
                        IsActive       = true,
                        CreatedAt      = DateTime.Now
                    });
                    Console.WriteLine($"[Seeder] Added subscription plan: {def.Name}");
                }
                else
                {
                    // Update existing plan features/price to match latest definition
                    existing.Description    = def.Description;
                    existing.Price          = def.Price;
                    existing.DurationMonths = def.Duration;
                    existing.UserLimit      = def.UserLimit;
                    existing.Features       = def.Features;
                    existing.IsActive       = true;
                    existing.UpdatedAt      = DateTime.Now;
                    Console.WriteLine($"[Seeder] Updated subscription plan: {def.Name}");
                }
            }

            await context.SaveChangesAsync();
        }

    }
}
