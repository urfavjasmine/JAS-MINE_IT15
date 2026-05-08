using JAS_MINE_IT15.Models.Entities;
using System.Security.Claims;

namespace JAS_MINE_IT15.Services
{
    /// <summary>
    /// CONTACT MASKING HELPER
    /// =====================
    /// Masks sensitive contact information (email + phone) for display in views and APIs.
    /// Database stores REAL data (encrypted or plain), this helper masks it for viewing.
    /// 
    /// PURPOSE:
    /// - Show "con***@domain.com" instead of full email to regular users
    /// - Show "0912****" instead of full phone to regular users
    /// - Allow admins to see real contact info
    /// - Provide hints for UI display
    /// </summary>
    public static class MaskedContactHelper
    {
        /// <summary>
        /// Contact information DTO - safe to return from API
        /// Contains both real and masked versions for role-based display
        /// </summary>
        public class ContactInfo
        {
            /// <summary>Real email (show to admins/owners only)</summary>
            public string? Email { get; set; }

            /// <summary>Real phone (show to admins/owners only)</summary>
            public string? Phone { get; set; }

            /// <summary>Masked email (always safe for display)</summary>
            public string? MaskedEmail { get; set; }

            /// <summary>Masked phone (always safe for display)</summary>
            public string? MaskedPhone { get; set; }

            /// <summary>Email hint like "contact@..." for UI</summary>
            public string? EmailHint { get; set; }

            /// <summary>Phone hint like "0912****" for UI</summary>
            public string? PhoneHint { get; set; }
        }

        /// <summary>
        /// Get masked contact information from Barangay
        /// Application gets plaintext (decrypted automatically), helper masks it for display
        /// </summary>
        public static ContactInfo GetMaskedBarangayContact(Barangay? barangay)
        {
            if (barangay == null)
            {
                return new ContactInfo
                {
                    Email = null,
                    Phone = null,
                    MaskedEmail = "***@***",
                    MaskedPhone = "****",
                    EmailHint = null,
                    PhoneHint = null
                };
            }

            return new ContactInfo
            {
                Email = barangay.ContactEmail,
                Phone = barangay.ContactPhone,
                MaskedEmail = DataMaskingHelper.MaskEmail(barangay.ContactEmail),
                MaskedPhone = DataMaskingHelper.MaskPhoneNumber(barangay.ContactPhone),
                EmailHint = GetEmailHint(barangay.ContactEmail),
                PhoneHint = GetPhoneHint(barangay.ContactPhone)
            };
        }

        /// <summary>
        /// Get masked contact information from User
        /// </summary>
        public static ContactInfo GetMaskedUserContact(User? user)
        {
            if (user == null)
            {
                return new ContactInfo
                {
                    Email = null,
                    Phone = null,
                    MaskedEmail = "***@***",
                    MaskedPhone = "****",
                    EmailHint = null,
                    PhoneHint = null
                };
            }

            return new ContactInfo
            {
                Email = user.Email,
                Phone = user.PhoneNumber,
                MaskedEmail = DataMaskingHelper.MaskEmail(user.Email),
                MaskedPhone = DataMaskingHelper.MaskPhoneNumber(user.PhoneNumber),
                EmailHint = GetEmailHint(user.Email),
                PhoneHint = GetPhoneHint(user.PhoneNumber)
            };
        }

        /// <summary>
        /// Get email hint for display: "john@..." (first 2 chars of local part)
        /// </summary>
        public static string? GetEmailHint(string? email)
        {
            if (string.IsNullOrEmpty(email) || !email.Contains('@'))
                return null;

            var parts = email.Split('@', 2);
            var local = parts[0];
            var domain = parts[1];

            if (local.Length <= 2)
                return $"{local[0]}*@{domain}";

            return $"{local.Substring(0, 2)}***@{domain}";
        }

        /// <summary>
        /// Get phone hint for display: "0912****" (first 4 digits visible)
        /// </summary>
        public static string? GetPhoneHint(string? phone)
        {
            if (string.IsNullOrEmpty(phone) || phone.Length < 4)
                return "****";

            return phone.Substring(0, 4) + new string('*', phone.Length - 4);
        }

        /// <summary>
        /// Check if user role should see real contact information
        /// Returns true for: super_admin, barangay_admin, or if viewing own data
        /// </summary>
        public static bool CanViewRealContact(string? userRole, bool isOwnData = false)
        {
            // Users can always see their own contact info
            if (isOwnData)
                return true;

            // Only admins can see others' contact info
            return userRole == "super_admin" || userRole == "barangay_admin";
        }

        /// <summary>
        /// Apply role-based masking to contact info
        /// Replaces real data with masked if user doesn't have permission
        /// </summary>
        public static void ApplyRoleBasedMasking(ContactInfo contact, string? userRole, bool isOwnData = false)
        {
            if (!CanViewRealContact(userRole, isOwnData))
            {
                // Non-privileged users see only masked data
                contact.Email = contact.MaskedEmail;
                contact.Phone = contact.MaskedPhone;
            }
        }

        /// <summary>
        /// Get appropriate contact info for API response based on user role
        /// </summary>
        public static ContactInfo GetApiContactResponse(Barangay? barangay, ClaimsPrincipal? user)
        {
            var contact = GetMaskedBarangayContact(barangay);
            var userRole = user?.FindFirst(ClaimTypes.Role)?.Value;
            var isOwnData = false; // Set to true if checking owner's own data

            ApplyRoleBasedMasking(contact, userRole, isOwnData);
            return contact;
        }

        /// <summary>
        /// Get appropriate contact info for API response based on user role
        /// </summary>
        public static ContactInfo GetApiContactResponse(User? userData, ClaimsPrincipal? user)
        {
            var contact = GetMaskedUserContact(userData);
            var userRole = user?.FindFirst(ClaimTypes.Role)?.Value;

            // Check if viewing own user data
            var userIdClaim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isOwnData = userData?.Id.ToString() == userIdClaim;

            ApplyRoleBasedMasking(contact, userRole, isOwnData);
            return contact;
        }
    }

    /// <summary>
    /// Extension methods for easy inline access to masked contact info
    /// </summary>
    public static class MaskedContactExtensions
    {
        /// <summary>
        /// Get masked contact info from Barangay
        /// Usage: var masked = barangay.GetMaskedContact();
        /// </summary>
        public static MaskedContactHelper.ContactInfo GetMaskedContact(this Barangay barangay)
        {
            return MaskedContactHelper.GetMaskedBarangayContact(barangay);
        }

        /// <summary>
        /// Get masked contact info from User
        /// Usage: var masked = user.GetMaskedContact();
        /// </summary>
        public static MaskedContactHelper.ContactInfo GetMaskedContact(this User user)
        {
            return MaskedContactHelper.GetMaskedUserContact(user);
        }

        /// <summary>
        /// Get email hint from Barangay
        /// Usage: var hint = barangay.GetEmailHint(); // "contact@..."
        /// </summary>
        public static string? GetEmailHint(this Barangay? barangay)
        {
            return MaskedContactHelper.GetEmailHint(barangay?.ContactEmail);
        }

        /// <summary>
        /// Get phone hint from Barangay
        /// Usage: var hint = barangay.GetPhoneHint(); // "0912****"
        /// </summary>
        public static string? GetPhoneHint(this Barangay? barangay)
        {
            return MaskedContactHelper.GetPhoneHint(barangay?.ContactPhone);
        }

        /// <summary>
        /// Get email hint from User
        /// Usage: var hint = user.GetEmailHint();
        /// </summary>
        public static string? GetEmailHint(this User? user)
        {
            return MaskedContactHelper.GetEmailHint(user?.Email);
        }

        /// <summary>
        /// Get phone hint from User
        /// Usage: var hint = user.GetPhoneHint(); // "0912****"
        /// </summary>
        public static string? GetPhoneHint(this User? user)
        {
            return MaskedContactHelper.GetPhoneHint(user?.PhoneNumber);
        }
    }
}
