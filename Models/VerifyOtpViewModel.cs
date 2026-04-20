using System.ComponentModel.DataAnnotations;

namespace JAS_MINE_IT15.Models
{
    public class VerifyOtpViewModel
    {
        [Required(ErrorMessage = "Verification code is required.")]
        [StringLength(8, MinimumLength = 4, ErrorMessage = "Verification code must be 4 to 8 characters.")]
        public string Code { get; set; } = string.Empty;

        public string MaskedEmail { get; set; } = string.Empty;

        public int RemainingSeconds { get; set; }

        public string ErrorMessage { get; set; } = string.Empty;
    }
}
