namespace Backend.DTOs.ProfileController
{
    public class UpdateProfileUserDTO
    {
        public string? Username { get; set; }
        public string? Description { get; set; }
        public bool? Privacy { get; set; }
        public string? ProfilePictureUrl { get; set; }
    }
}