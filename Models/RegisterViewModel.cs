using System.ComponentModel.DataAnnotations;

namespace JAS_MINE_IT15.Models
{
    public class RegisterViewModel
    {
        // --- Step 1: Personal Details ---
        [Required(ErrorMessage = "First Name is required.")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last Name is required.")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email Address is required.")]
        [EmailAddress(ErrorMessage = "Invalid Email Address.")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Phone Number")]
        [StringLength(11, MinimumLength = 11, ErrorMessage = "Phone number must be exactly 11 digits.")]
        [RegularExpression(@"^09\d{9}$", ErrorMessage = "Invalid Philippine phone number format (e.g. 09123456789).")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 12)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{12,}$", ErrorMessage = "Password must be at least 12 characters and include uppercase, lowercase, number, and special character.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your password.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        // --- Step 2: Barangay Details ---
        [Required(ErrorMessage = "Barangay Name is required.")]
        [Display(Name = "Barangay Name")]
        public string BarangayName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Municipality/City is required.")]
        [Display(Name = "Municipality/City")]
        public string Municipality { get; set; } = string.Empty;

        [Required(ErrorMessage = "Province is required.")]
        [Display(Name = "Province")]
        public string Province { get; set; } = string.Empty;

        [Required(ErrorMessage = "Region is required.")]
        [Display(Name = "Region")]
        public string Region { get; set; } = string.Empty;

        [Display(Name = "Complete Address")]
        public string? Address { get; set; }

        // --- Helpers ---
        public string? ErrorMessage { get; set; }
        
        // Form persistence
        public int CurrentStep { get; set; } = 1;
    }
}
