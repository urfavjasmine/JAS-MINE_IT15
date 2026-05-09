using System.ComponentModel.DataAnnotations;

namespace JAS_MINE_IT15.Validations
{
    /// <summary>
    /// Validates that a value is a valid email address format.
    /// Built-in [EmailAddress] attribute but with stricter validation.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
    public class ValidEmailAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value is null || string.IsNullOrWhiteSpace(value?.ToString()))
                return true; // Allow null/empty (use [Required] separately if needed)

            var email = value!.ToString()!;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                // Additional check: disallow plus addressing for security (user+tag@example.com)
                return !email.Contains("+", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        public override string FormatErrorMessage(string name) =>
            $"The {name} field must be a valid email address.";
    }

    /// <summary>
    /// Validates that a password meets strong requirements.
    /// Requires: 12+ chars, uppercase, lowercase, digit, special char, 4+ unique chars.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
    public class StrongPasswordAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value is null || string.IsNullOrWhiteSpace(value?.ToString()))
                return false; // Password should be required

            var password = value!.ToString()!;

            // Check minimum length
            if (password.Length < 12)
                return false;

            // Check maximum length
            if (password.Length > 128)
                return false;

            // Check for uppercase
            if (!password.Any(char.IsUpper))
                return false;

            // Check for lowercase
            if (!password.Any(char.IsLower))
                return false;

            // Check for digit
            if (!password.Any(char.IsDigit))
                return false;

            // Check for special character
            if (!password.Any(c => !char.IsLetterOrDigit(c)))
                return false;

            // Check unique characters (at least 4)
            var uniqueChars = password.Distinct().Count();
            if (uniqueChars < 4)
                return false;

            return true;
        }

        public override string FormatErrorMessage(string name) =>
            $"The {name} must be at least 12 characters and contain uppercase, lowercase, digit, and special character.";
    }

    /// <summary>
    /// Validates that a phone number format is valid (Philippine format).
    /// Accepts: 09XXXXXXXXX (11 digits), +639XXXXXXXXX, (09) XXX-XXXX, etc.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
    public class ValidPhoneNumberAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value is null || string.IsNullOrWhiteSpace(value.ToString()))
                return true; // Allow null/empty (use [Required] separately if needed)

            var phone = value.ToString()!;

            // Remove common formatting characters
            var cleaned = System.Text.RegularExpressions.Regex.Replace(phone, @"[\s\-\(\)\.+]", "");

            // Check if it's a valid Philippine mobile number
            // Starts with 63 (country code) or 09 (local)
            // Must be 11-12 digits total
            var isValid = System.Text.RegularExpressions.Regex.IsMatch(
                cleaned,
                @"^(63|0)9\d{9}$" // 639123456789 (12) or 09123456789 (11)
            );

            return isValid;
        }

        public override string FormatErrorMessage(string name) =>
            $"The {name} must be a valid Philippine phone number (e.g., 09123456789 or +639123456789).";
    }

    /// <summary>
    /// Validates that a value is not null/empty and within a string length range.
    /// More flexible than built-in [StringLength].
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
    public class ValidStringLengthAttribute : ValidationAttribute
    {
        private readonly int _minLength;
        private readonly int _maxLength;

        public ValidStringLengthAttribute(int minLength, int maxLength)
        {
            if (minLength < 0) throw new ArgumentException("Minimum length cannot be negative.", nameof(minLength));
            if (maxLength < minLength) throw new ArgumentException("Maximum length must be >= minimum length.", nameof(maxLength));

            _minLength = minLength;
            _maxLength = maxLength;
        }

        public override bool IsValid(object? value)
        {
            if (value is null || string.IsNullOrWhiteSpace(value.ToString()))
                return _minLength == 0; // Valid if min is 0

            var stringValue = value.ToString()!.Trim();
            return stringValue.Length >= _minLength && stringValue.Length <= _maxLength;
        }

        public override string FormatErrorMessage(string name) =>
            $"The {name} must be between {_minLength} and {_maxLength} characters.";
    }

    /// <summary>
    /// Validates that a file name has an allowed extension.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
    public class ValidFileExtensionAttribute : ValidationAttribute
    {
        private readonly string[] _allowedExtensions;

        public ValidFileExtensionAttribute(params string[] allowedExtensions)
        {
            _allowedExtensions = allowedExtensions.Select(e => e.ToLowerInvariant().TrimStart('.')).ToArray();

            if (_allowedExtensions.Length == 0)
                throw new ArgumentException("At least one extension must be specified.", nameof(allowedExtensions));
        }

        public override bool IsValid(object? value)
        {
            if (value is null || string.IsNullOrWhiteSpace(value.ToString()))
                return true; // Allow null/empty (use [Required] separately)

            var fileName = value.ToString()!;
            var ext = System.IO.Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();

            return _allowedExtensions.Contains(ext);
        }

        public override string FormatErrorMessage(string name) =>
            $"The {name} must have one of these extensions: {string.Join(", ", _allowedExtensions)}.";
    }

    /// <summary>
    /// Validates that a value is within a numeric range.
    /// More flexible than built-in [Range].
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
    public class ValidRangeAttribute : ValidationAttribute
    {
        private readonly int _minValue;
        private readonly int _maxValue;

        public ValidRangeAttribute(int minValue, int maxValue)
        {
            if (maxValue < minValue)
                throw new ArgumentException("Maximum value must be >= minimum value.", nameof(maxValue));

            _minValue = minValue;
            _maxValue = maxValue;
        }

        public override bool IsValid(object? value)
        {
            if (value is null)
                return true; // Allow null (use [Required] separately)

            if (!int.TryParse(value.ToString(), out var intValue))
                return false;

            return intValue >= _minValue && intValue <= _maxValue;
        }

        public override string FormatErrorMessage(string name) =>
            $"The {name} must be between {_minValue} and {_maxValue}.";
    }
}
