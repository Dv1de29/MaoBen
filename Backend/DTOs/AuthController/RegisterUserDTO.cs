using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs.AuthController
{
    public class RegisterUserDTO
    {
        [Required]
        public required string FirstName { get; set; }

        [Required]
        public required string LastName { get; set; }

        [Required]
        public required string Username { get; set; }
        
        [Required]
        [EmailAddress]
        public required string Email { get; set; }
       
        [Required]
        [MinLength(6)] 
        public required string Password { get; set; }

        [Required]
        [Compare("Password", ErrorMessage = "Passwords do not match!")]
        public required string ConfirmPassword { get; set; }
    }
}