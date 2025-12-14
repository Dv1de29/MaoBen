using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs.AuthController
{
    public class LoginUserDTO
    {
        [Required]
        public string UsernameOrEmail { get; set; } = string.Empty;
        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
