using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    public class RegisterUserDTO
    {
        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        public string Username { get; set; } // User chooses this!

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [MinLength(6)] 
        public string Password { get; set; }

        [Required]
        [Compare("Password", ErrorMessage = "Passwords do not match")] // Handles the "Password Twice" check automatically!
        public string ConfirmPassword { get; set; }
    }
}