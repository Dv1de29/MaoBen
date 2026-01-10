using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs.AuthController
{
    public class LoginUserDTO
    {
        [Required]
        public required string UsernameOrEmail { get; set; } = string.Empty;
        
        [Required]
        public required string Password { get; set; } = string.Empty;
    }
}
