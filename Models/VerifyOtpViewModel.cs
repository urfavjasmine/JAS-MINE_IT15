using System.ComponentModel.DataAnnotations;

namespace JAS_MINE_IT15.Models
{
    public class VerifyOtpViewModel
    {
        [Required(ErrorMessage = "Verification code or recovery code is required.")]
        [StringLength(32, MinimumLength = 6, ErrorMessage = "Enter a valid 6-digit OTP or recovery code.")]
        public string Code { get; set; } = string.Empty;

        public string MaskedEmail { get; set; } = string.Empty;

        public int RemainingSeconds { get; set; }

        public int ResendAvailableInSeconds { get; set; }

        public int RemainingResends { get; set; }

        public bool CanResend { get; set; }

        public bool RememberDevice { get; set; }

        public string ErrorMessage { get; set; } = string.Empty;

        public string SuccessMessage { get; set; } = string.Empty;
    }
}
