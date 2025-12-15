using Microsoft.AspNetCore.Identity;

namespace Backend.Models
{
    public class ApplicationUser : IdentityUser
    {
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public string Description { get; set; } = string.Empty;
        public string ProfilePictureUrl { get; set; } = "/assets/img/no_user.png"; //ar trebui sa punem o ruta de la poza default aici
        public bool Privacy { get; set; } = false;
        public string FullName => $"{FirstName} {LastName}";
    }
}