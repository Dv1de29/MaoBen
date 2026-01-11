namespace Backend.DTOs.FollowController
{
    public class PendingRequestResponseDTO
    {
        public required string Username { get; set; }
        public required string ProfilePictureUrl { get; set; }
    }
}