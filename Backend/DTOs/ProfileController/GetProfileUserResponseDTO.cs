namespace Backend.DTOs.ProfileController
{
    public class GetProfileUserResponseDTO
    {
        public required string Username { get; set; }
        public required string Email { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public bool Privacy { get; set; }
        public int FollowersCount { get; set; }
        public int FollowingCount { get; set; }
        public string? Description { get; set; }
    }
}
