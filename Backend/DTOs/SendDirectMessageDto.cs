namespace Backend.DTOs.DirectMessageController
{
    /// <summary>
    /// DTO pentru trimiterea unui mesaj direct
    /// Frontend: POST /api/directmessages/send/{recipientId}
    /// Body: { "content": "Hello!" }
    /// </summary>
    public class SendDirectMessageDto
    {
        public string Content { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO pentru afi?area unui mesaj direct în conversa?ie
    /// Frontend: Used when displaying messages in a conversation
    /// </summary>
    public class DirectMessageDto
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public string SenderId { get; set; }
        public string SenderUsername { get; set; }
        public string SenderProfilePictureUrl { get; set; }
        public bool IsMine { get; set; } // Ajut? frontend-ul s? ?tie dac? arat? mesajul la stânga sau la dreapta
    }

    /// <summary>
    /// DTO pentru afi?area unei conversa?ii (pereche de utilizatori)
    /// Frontend: Used in conversations list
    /// </summary>
    public class ConversationDto
    {
        public string OtherUserId { get; set; }
        public string OtherUserUsername { get; set; }
        public string OtherUserProfilePictureUrl { get; set; }
        public string? LastMessagePreview { get; set; } // Ultimul mesaj pentru preview
        public DateTime? LastMessageTime { get; set; } // Ora ultimului mesaj
        public int UnreadCount { get; set; } // Num?rul de mesaje necitite
    }

    /// <summary>
    /// DTO pentru marcare mesaj ca citit
    /// Frontend: PATCH /api/directmessages/{messageId}/mark-as-read
    /// </summary>
    public class MarkMessageAsReadDto
    {
        // No properties needed - just triggers the mark as read action
    }
}