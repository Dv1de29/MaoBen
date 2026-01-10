namespace Backend.DTOs.DirectMessageController
{
    /// <summary>
    /// Enum pentru statusul mesajelor (optional pentru future features)
    /// </summary>
    public enum MessageStatus
    {
        Sent = 0,
        Delivered = 1,
        Read = 2
    }

    /// <summary>
    /// DTO pentru typing indicator (utilizator scrie în prezent)
    /// Frontend: SignalR event - "UserTyping" { "senderId": "...", "isTyping": true }
    /// </summary>
    public class TypingIndicatorDto
    {
        public string UserId { get; set; }
        public bool IsTyping { get; set; }
    }
}