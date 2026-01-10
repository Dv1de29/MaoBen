using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Backend.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [MaxLength(50)] 
        public required string FirstName { get; set; }

        [Required]
        [MaxLength(50)]
        public required string LastName { get; set; }

        [Required] 
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public string ProfilePictureUrl { get; set; } = "/assets/img/no_user.png"; //Default picture for users

        public bool IsPrivate { get; set; } = false;

        public int FollowersCount { get; set; } = 0;

        public int FollowingCount { get; set; } = 0;

        public string FullName => $"{FirstName} {LastName}";
    }
}