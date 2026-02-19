using Microsoft.AspNetCore.Identity;

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

            string email = "superadmin@jas-mine.gov.ph";
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

    }
}
