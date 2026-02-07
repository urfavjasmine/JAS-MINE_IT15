using System.ComponentModel.DataAnnotations;

namespace JAS_MINE_IT15.Models
{
    public class LoginViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        public string Password { get; set; } = "";

        [Required] 
        public string Role { get; set; } = "";

        public string ErrorMessage { get; set; } = "";
    }
}
