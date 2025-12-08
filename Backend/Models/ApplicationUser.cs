using Microsoft.AspNetCore.Identity;

namespace Backend.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string ProfilePictureUrl { get; set; } = string.Empty;

        public bool IsPrivate { get; set; } = false;

        public string FullName => $"{FirstName} {LastName}";
    }
}