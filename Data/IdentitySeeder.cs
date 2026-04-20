using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using JAS_MINE_IT15.Models.Entities;
using Microsoft.Extensions.Configuration;

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
                "admin",
                "barangay_admin",
                "user",
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
            var context = services.GetRequiredService<ApplicationDbContext>();
            var config = services.GetRequiredService<IConfiguration>();
            var loggerFactory = services.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("IdentitySeeder");

            var superAdminSeeds = config.GetSection("IdentitySeed:SuperAdmins").Get<List<SuperAdminSeed>>()
                                  ?? new List<SuperAdminSeed>();

            foreach (var seed in superAdminSeeds)
            {
                var email = (seed.Email ?? string.Empty).Trim().ToLower();
                if (string.IsNullOrWhiteSpace(email))
                {
                    continue;
                }

                var identityUser = await userManager.FindByEmailAsync(email);
                if (identityUser == null)
                {
                    if (string.IsNullOrWhiteSpace(seed.Password))
                    {
                        logger.LogWarning("Skipping super_admin seed for {Email}: user does not exist and no password was provided.", email);
                        continue;
                    }

                    identityUser = new IdentityUser
                    {
                        UserName = email,
                        Email = email,
                        EmailConfirmed = true
                    };

                    var createResult = await userManager.CreateAsync(identityUser, seed.Password);
                    if (!createResult.Succeeded)
                    {
                        logger.LogWarning("Failed creating seeded super_admin {Email}: {Errors}", email,
                            string.Join(", ", createResult.Errors.Select(e => e.Description)));
                        continue;
                    }

                    logger.LogInformation("Created Identity user for seeded super_admin {Email}.", email);
                }

                if (!await userManager.IsInRoleAsync(identityUser, "super_admin"))
                {
                    var roleResult = await userManager.AddToRoleAsync(identityUser, "super_admin");
                    if (!roleResult.Succeeded)
                    {
                        logger.LogWarning("Failed assigning super_admin role to {Email}: {Errors}", email,
                            string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                        continue;
                    }
                }

                var businessUser = await context.BusinessUsers.FirstOrDefaultAsync(u => u.Email.ToLower() == email);
                if (businessUser == null)
                {
                    context.BusinessUsers.Add(new User
                    {
                        Email = email,
                        PasswordHash = "IDENTITY_MANAGED",
                        FullName = string.IsNullOrWhiteSpace(seed.FullName) ? email : seed.FullName.Trim(),
                        Role = "super_admin",
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    });
                }
                else
                {
                    businessUser.Role = "super_admin";
                    businessUser.IsActive = true;
                    if (!string.IsNullOrWhiteSpace(seed.FullName))
                    {
                        businessUser.FullName = seed.FullName.Trim();
                    }
                    businessUser.UpdatedAt = DateTime.Now;
                }
            }

            await context.SaveChangesAsync();
        }

        private sealed class SuperAdminSeed
        {
            public string? Email { get; set; }
            public string? Password { get; set; }
            public string? FullName { get; set; }
        }
        public static async Task SeedDefaultUsers(IServiceProvider services)
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// Seeds a test barangay and links all seeded default users to it (BusinessUsers table).
        /// Ensures test accounts have a valid BarangayId so the full subscription flow works.
        /// </summary>
        public static async Task SeedTestBarangayAndLinkUsers(ApplicationDbContext context)
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// Seeds 3 subscription plans: Basic, Professional, and Enterprise (monthly).
        /// </summary>
        public static async Task SeedSubscriptionPlans(ApplicationDbContext context)
        {
            // Delete ALL old/invalid plans (plans with wrong prices or wrong names)
            var wantedPlans = new Dictionary<string, decimal>
            {
                { "Basic", 299.00m },
                { "Professional", 599.00m },
                { "Enterprise", 999.00m }
            };

            var allPlans = await context.SubscriptionPlans.ToListAsync();
            foreach (var plan in allPlans)
            {
                // Delete if not in wanted list or has wrong price
                var shouldDelete = !wantedPlans.ContainsKey(plan.Name) || plan.Price != wantedPlans[plan.Name];
                if (shouldDelete)
                {
                    var hasSubscriptions = await context.BarangaySubscriptions
                        .AnyAsync(s => s.PlanId == plan.Id);

                    if (!hasSubscriptions)
                    {
                        context.SubscriptionPlans.Remove(plan);
                        Console.WriteLine($"[Seeder] Deleted invalid plan: {plan.Name} (₱{plan.Price:N0})");
                    }
                    else
                    {
                        plan.IsActive = false;
                        plan.UpdatedAt = DateTime.Now;
                        Console.WriteLine($"[Seeder] Deactivated invalid plan: {plan.Name} (has subscriptions)");
                    }
                }
            }
            await context.SaveChangesAsync();

            var planDefs = new[]
            {
                new
                {
                    Name        = "Basic",
                    Description = "Essential tools for small barangays getting started.",
                    Price       = 299.00m,
                    Duration    = 1,
                    UserLimit   = 4,
                    Features    = string.Join(";",
                        "Up to 4 users",
                        "View records",
                        "Add and manage records",
                        "View announcements",
                        "Basic reports")
                },
                new
                {
                    Name        = "Professional",
                    Description = "Everything you need to manage your barangay records efficiently.",
                    Price       = 599.00m,
                    Duration    = 1,
                    UserLimit   = 10,
                    Features    = string.Join(";",
                        "Up to 10 users",
                        "All Basic features",
                        "Create and manage announcements",
                        "Better reports",
                        "Activity logs")
                },
                new
                {
                    Name        = "Enterprise",
                    Description = "Complete access with advanced tools and detailed tracking.",
                    Price       = 999.00m,
                    Duration    = 1,
                    UserLimit   = 20,
                    Features    = string.Join(";",
                        "Up to 20 users",
                        "All Professional features",
                        "Dashboard (summary view)",
                        "Archive and restore data",
                        "Detailed tracking")
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
                    Console.WriteLine($"[Seeder] Added subscription plan: {def.Name} ({def.Price})");
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
                    Console.WriteLine($"[Seeder] Updated subscription plan: {def.Name} ({def.Price})");
                }
            }

            await context.SaveChangesAsync();
        }

    }
}
