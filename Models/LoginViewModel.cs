using System.ComponentModel.DataAnnotations;

namespace JAS_MINE_IT15.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email is required.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; }
        public string ErrorMessage { get; set; } = "";
    }
}
