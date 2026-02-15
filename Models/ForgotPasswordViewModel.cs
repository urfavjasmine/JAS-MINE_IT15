using System.ComponentModel.DataAnnotations;

namespace JAS_MINE_IT15.Models
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string Email { get; set; } = "";

        public bool Submitted { get; set; } = false;

        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
