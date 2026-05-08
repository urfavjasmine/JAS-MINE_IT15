using System;
using System.Text.RegularExpressions;

namespace JAS_MINE_IT15.Services
{
    /// <summary>
    /// Centralized data masking utility for PII and sensitive information display.
    /// Used for audit logs, views, and report generation.
    /// Does NOT modify database storage - only affects display/logging.
    /// </summary>
    public static class DataMaskingHelper
    {
        /// <summary>
        /// Mask email: "john.doe@example.com" → "jo*****@example.com"
        /// </summary>
        public static string MaskEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
                return "***@***";

            var parts = email.Split('@', 2);
            var name = parts[0];
            var domain = parts[1];
            var maskedName = name.Length <= 2
                ? name[0] + "*"
                : name.Substring(0, 2) + new string('*', Math.Max(1, name.Length - 2));
            return $"{maskedName}@{domain}";
        }

        /// <summary>
        /// Mask phone: "09123456789" → "0912****6789"
        /// </summary>
        public static string MaskPhoneNumber(string? phone)
        {
            if (string.IsNullOrEmpty(phone) || phone.Length < 4)
                return "****";

            var firstPart = phone[..Math.Min(4, phone.Length)];
            var lastPart = phone[Math.Max(0, phone.Length - 4)..];
            var masked = new string('*', Math.Max(0, phone.Length - 8));
            return $"{firstPart}{masked}{lastPart}";
        }

        /// <summary>
        /// Mask credit card: "1234567890123456" → "****3456"
        /// </summary>
        public static string MaskCreditCard(string? card)
        {
            if (string.IsNullOrEmpty(card) || card.Length < 4)
                return "****";
            return new string('*', card.Length - 4) + card[^4..];
        }

        /// <summary>
        /// Mask full name: "Juan Dela Cruz" → "Juan D. C."
        /// </summary>
        public static string MaskFullName(string? name)
        {
            if (string.IsNullOrEmpty(name))
                return "***";

            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return "***";
            if (parts.Length == 1)
                return parts[0];

            return parts[0] + " " + string.Join(". ", parts.Skip(1).Select(p => p[0])) + ".";
        }

        /// <summary>
        /// Mask IP address: "192.168.1.100" → "192.168.1.***"
        /// </summary>
        public static string MaskIpAddress(string? ip)
        {
            if (string.IsNullOrEmpty(ip))
                return "***";

            var parts = ip.Split('.');
            if (parts.Length != 4)
                return ip; // Not IPv4, return as-is

            return $"{parts[0]}.{parts[1]}.{parts[2]}.***";
        }

        /// <summary>
        /// Mask generic string: show first 2 chars, hide rest
        /// </summary>
        public static string MaskGeneric(string? value, int showFirst = 2, char maskChar = '*')
        {
            if (string.IsNullOrEmpty(value) || value.Length <= showFirst)
                return "***";

            return value[..showFirst] + new string(maskChar, value.Length - showFirst);
        }

        /// <summary>
        /// Mask sensitive PII in text using regex patterns (emails, phones, etc.)
        /// </summary>
        public static string MaskSensitiveInformation(string? text)
        {
            if (string.IsNullOrEmpty(text))
                return text ?? "";

            // Mask emails: example@domain.com → ex***@domain.com
            var emailPattern = @"[\w\.-]+@[\w\.-]+\.\w+";
            text = Regex.Replace(text, emailPattern, m => MaskEmail(m.Value), RegexOptions.IgnoreCase);

            // Mask Philippine phone numbers: 09XXXXXXXXX → 0912****XXXX
            var phonePattern = @"\b09\d{9}\b";
            text = Regex.Replace(text, phonePattern, m => MaskPhoneNumber(m.Value));

            return text;
        }
    }
}
