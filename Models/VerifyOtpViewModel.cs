using System.ComponentModel.DataAnnotations;

namespace JAS_MINE_IT15.Models
{
    public class VerifyOtpViewModel
    {
        [Required(ErrorMessage = "Verification code is required.")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Verification code must be exactly 6 digits.")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Verification code must be exactly 6 digits.")]
        public string Code { get; set; } = string.Empty;

        public string MaskedEmail { get; set; } = string.Empty;

        public int RemainingSeconds { get; set; }

        public string ErrorMessage { get; set; } = string.Empty;
    }
}
