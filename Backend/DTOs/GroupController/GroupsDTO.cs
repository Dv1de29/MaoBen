namespace Backend.DTOs.GroupController
{
    // Pentru crearea unui grup
    public class CreateGroupDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    // Pentru afișarea unui grup în listă
    public class GroupDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string OwnerUsername { get; set; }
        public bool IsUserMember { get; set; } // Ajută frontend-ul să știe dacă arată buton de Join sau Leave
    }

    // Pentru trimiterea unui mesaj
    public class SendMessageDto
    {
        public string Content { get; set; }
    }

    // Pentru afișarea unui mesaj
    public class GroupMessageDto
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Username { get; set; } // Cine a scris
        public string ProfilePictureUrl { get; set; }
        public bool IsMine { get; set; } // E mesajul meu? (Pot să-l șterg?)
    }

    // Pentru cererile de Join (văzute de moderator)
    public class GroupRequestDto
    {
        public string UserId { get; set; }
        public string Username { get; set; }
        public string ProfilePictureUrl { get; set; }
    }
}