using Microsoft.AspNetCore.Identity;
using System.Text.RegularExpressions;

namespace JAS_MINE_IT15.Services
{
    /// <summary>
    /// Additional password validation layered on top of Identity options.
    /// Blocks common/weak passwords and predictable patterns.
    /// </summary>
    public class StrongPasswordValidator : IPasswordValidator<IdentityUser>
    {
        private static readonly HashSet<string> CommonPasswords = new(StringComparer.OrdinalIgnoreCase)
        {
            "password", "password123", "admin", "admin123", "qwerty", "qwerty123",
            "letmein", "welcome", "iloveyou", "abc123", "123456", "12345678",
            "123456789", "1234567890", "111111", "000000", "pass@123", "p@ssw0rd"
        };

        private static readonly string[] CommonSequences =
        {
            "qwerty", "asdf", "zxcv", "12345", "54321", "abcdef"
        };

        public Task<IdentityResult> ValidateAsync(UserManager<IdentityUser> manager, IdentityUser user, string? password)
        {
            var errors = new List<IdentityError>();
            var value = password ?? string.Empty;

            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add(new IdentityError
                {
                    Code = "PasswordEmpty",
                    Description = "Password cannot be empty."
                });
                return Task.FromResult(IdentityResult.Failed(errors.ToArray()));
            }

            if (value.Length > 128)
            {
                errors.Add(new IdentityError
                {
                    Code = "PasswordTooLong",
                    Description = "Password cannot exceed 128 characters."
                });
            }

            if (CommonPasswords.Contains(value))
            {
                errors.Add(new IdentityError
                {
                    Code = "PasswordTooCommon",
                    Description = "This password is too common. Please choose a stronger password."
                });
            }

            var normalized = value.ToLowerInvariant();
            if (CommonSequences.Any(seq => normalized.Contains(seq)))
            {
                errors.Add(new IdentityError
                {
                    Code = "PasswordContainsSequence",
                    Description = "Password must not contain common keyboard or numeric sequences."
                });
            }

            if (Regex.IsMatch(value, "(.)\\1{3,}"))
            {
                errors.Add(new IdentityError
                {
                    Code = "PasswordRepeatedChars",
                    Description = "Password must not contain repeated characters 4 or more times in a row."
                });
            }

            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                var emailLocalPart = user.Email.Split('@').FirstOrDefault() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(emailLocalPart)
                    && normalized.Contains(emailLocalPart.ToLowerInvariant(), StringComparison.Ordinal))
                {
                    errors.Add(new IdentityError
                    {
                        Code = "PasswordContainsEmail",
                        Description = "Password must not contain parts of your email address."
                    });
                }
            }

            if (!string.IsNullOrWhiteSpace(user.UserName)
                && normalized.Contains(user.UserName.ToLowerInvariant(), StringComparison.Ordinal))
            {
                errors.Add(new IdentityError
                {
                    Code = "PasswordContainsUsername",
                    Description = "Password must not contain your username."
                });
            }

            return errors.Count > 0
                ? Task.FromResult(IdentityResult.Failed(errors.ToArray()))
                : Task.FromResult(IdentityResult.Success);
        }
    }
}
