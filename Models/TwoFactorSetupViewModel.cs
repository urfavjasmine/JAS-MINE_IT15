using System.ComponentModel.DataAnnotations;

namespace JAS_MINE_IT15.Models
{
    public class TwoFactorSetupViewModel
    {
        public string SharedKey { get; set; } = "";
        public string QrCodeImageUrl { get; set; } = "";

        [Required(ErrorMessage = "Verification code is required.")]
        [StringLength(7, ErrorMessage = "Enter a valid code.")]
        public string Code { get; set; } = "";

        public string ErrorMessage { get; set; } = "";
    }
}
