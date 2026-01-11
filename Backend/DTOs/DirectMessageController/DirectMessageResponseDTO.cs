namespace Backend.DTOs.DirectMessageController
{
    public class DirectMessageResponseDTO
    {
        
            public int Id { get; set; }
            public required string Content { get; set; }
            public DateTime CreatedAt { get; set; }
            public required string SenderId { get; set; }
            public required string SenderUsername { get; set; }
            public required string SenderProfilePictureUrl { get; set; }
            public bool IsMine { get; set; } 
    }
}
