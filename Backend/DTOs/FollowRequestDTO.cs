namespace Backend.DTOs
{
    public class FollowRequestDTO
    {
        public string RequestId { get; set; } // ID-ul userului care cere
        public string Username { get; set; }
        public string? ProfilePictureUrl { get; set; }
    }
}