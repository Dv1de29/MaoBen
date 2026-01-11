namespace Backend.DTOs.GroupController
{
    public class PendingRequestsResponseDTO
    {
        public required string UserId { get; set; }
        public required string Username { get; set; }
        public required string ProfilePictureUrl { get; set; }
    }
}
