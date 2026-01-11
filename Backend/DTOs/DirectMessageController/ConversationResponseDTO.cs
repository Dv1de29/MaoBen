namespace Backend.DTOs.DirectMessageController
{
    public class ConversationResponseDTO
    {
        public required string OtherUserId { get; set; }
        public required string OtherUserUsername { get; set; }
        public required string OtherUserProfilePictureUrl { get; set; }
        public string? LastMessagePreview { get; set; } 
        public DateTime? LastMessageTime { get; set; } 
        public int UnreadCount { get; set; }
    }
}
