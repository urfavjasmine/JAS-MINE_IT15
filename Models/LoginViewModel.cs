using System.ComponentModel.DataAnnotations;

namespace JAS_MINE_IT15.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters.")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, ErrorMessage = "Password cannot exceed 100 characters.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        public string? RecaptchaToken { get; set; }
        public bool CaptchaRequired { get; set; }
        public string ErrorMessage { get; set; } = "";
    }
}
