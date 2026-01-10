using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs.AuthController
{
    public class LoginUserDTO
    {
        [Required(ErrorMessage = "Username or email is required.")]
        [MaxLength(256, ErrorMessage = "Username or email cannot exceed 256 characters.")]
        public required string UsernameOrEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        public required string Password { get; set; } = string.Empty;
    }
}