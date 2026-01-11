namespace Backend.DTOs.GroupController
{
    public class GroupMessageResponseDTO
    {
        public int Id { get; set; }
        public required string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public required string Username { get; set; } 
        public required string ProfilePictureUrl { get; set; }
        public bool IsMine { get; set; } 
    }
}
