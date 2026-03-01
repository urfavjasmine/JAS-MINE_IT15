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
                // Remove old password and set new one
                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                var resetResult = await userManager.ResetPasswordAsync(user, token, password);
                if (resetResult.Succeeded)
                {
                    Console.WriteLine($"[Seeder] Reset password for: {email}");
                }
                else
                {
                    Console.WriteLine($"[Seeder] FAILED to reset password for {email}: {string.Join(", ", resetResult.Errors.Select(e => e.Description))}");
                }
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
                else
                {
                    // Reset password if user already exists
                    var token = await userManager.GeneratePasswordResetTokenAsync(user);
                    await userManager.ResetPasswordAsync(user, token, d.Password);
                }

                if (!await userManager.IsInRoleAsync(user, d.Role))
                    await userManager.AddToRoleAsync(user, d.Role);
            }
        }

        /// <summary>
        /// Seeds 3 default subscription plans: Standard, Professional, Enterprise.
        /// Skips if plans with those names already exist.
        /// </summary>
        public static async Task SeedSubscriptionPlans(ApplicationDbContext context)
        {
            var planDefs = new[]
            {
                new
                {
                    Name        = "Standard",
                    Description = "Essential modules for barangay staff and council members to get started.",
                    Price       = 2999.00m,
                    Duration    = 12,
                    Features    = string.Join(";",
                        "👥 Roles: Staff, Council Member",
                        "📚 Knowledge Repository – View & download documents",
                        "📢 Announcements – View barangay-wide announcements",
                        "💡 Lessons Learned – Browse recorded lessons",
                        "🏆 Best Practices – Access the best practices database",
                        "🔒 Basic audit trail per user",
                        "📧 Email support")
                },
                new
                {
                    Name        = "Professional",
                    Description = "Full management tools for barangay admins and secretaries.",
                    Price       = 5999.00m,
                    Duration    = 12,
                    Features    = string.Join(";",
                        "👥 Roles: Admin, Secretary, Staff, Council",
                        "📚 Knowledge Repository – Upload, edit & organize documents",
                        "📄 Policy & Procedures – Create, approve & manage policies",
                        "💡 Lessons Learned – Create & share lessons",
                        "🏆 Best Practices – Contribute & manage practices",
                        "🔗 Knowledge Sharing – Start discussions & threads",
                        "📢 Announcements – Create & manage announcements",
                        "🔒 Full audit logs & activity tracking",
                        "⚡ Priority email & chat support")
                },
                new
                {
                    Name        = "Enterprise",
                    Description = "Complete ERP access for the entire barangay organization.",
                    Price       = 9999.00m,
                    Duration    = 12,
                    Features    = string.Join(";",
                        "👥 Roles: Admin, Secretary, Staff, Council",
                        "📚 Knowledge Repository – Full access with archive & restore",
                        "📄 Policy & Procedures – Full lifecycle management",
                        "💡 Lessons Learned – Full CRUD with archive & restore",
                        "🏆 Best Practices – Full CRUD with archive & restore",
                        "🔗 Knowledge Sharing – Full discussions & collaboration",
                        "📢 Announcements – Full management with scheduling",
                        "👤 User Management – Add, edit & deactivate users",
                        "🔒 Advanced audit logs with export",
                        "📊 Dashboard analytics & reporting",
                        "🛡️ Dedicated account manager & phone support")
                }
            };

            foreach (var def in planDefs)
            {
                var exists = await context.SubscriptionPlans
                    .AnyAsync(p => p.Name == def.Name);

                if (!exists)
                {
                    context.SubscriptionPlans.Add(new SubscriptionPlan
                    {
                        Name           = def.Name,
                        Description    = def.Description,
                        Price          = def.Price,
                        DurationMonths = def.Duration,
                        Features       = def.Features,
                        IsActive       = true,
                        CreatedAt      = DateTime.Now
                    });

                    Console.WriteLine($"[Seeder] Added subscription plan: {def.Name}");
                }
            }

            await context.SaveChangesAsync();
        }

    }
}
