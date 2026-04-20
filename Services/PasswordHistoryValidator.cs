using Microsoft.AspNetCore.Identity;

namespace JAS_MINE_IT15.Services
{
    public class PasswordHistoryValidator : IPasswordValidator<IdentityUser>
    {
        private readonly IPasswordHistoryService _passwordHistoryService;

        public PasswordHistoryValidator(IPasswordHistoryService passwordHistoryService)
        {
            _passwordHistoryService = passwordHistoryService;
        }

        public async Task<IdentityResult> ValidateAsync(UserManager<IdentityUser> manager, IdentityUser user, string? password)
        {
            if (user == null || string.IsNullOrWhiteSpace(user.Id) || string.IsNullOrWhiteSpace(password))
            {
                return IdentityResult.Success;
            }

            var reused = await _passwordHistoryService.IsPasswordReusedAsync(user, password, historyDepth: 5);
            if (!reused)
            {
                return IdentityResult.Success;
            }

            return IdentityResult.Failed(new IdentityError
            {
                Code = "PasswordReused",
                Description = "You cannot reuse your recent passwords. Please choose a new password."
            });
        }
    }
}
