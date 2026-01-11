namespace Backend.DTOs.GroupController
{
    public class GroupMemberResponseDTO
    {
        public string Username { get; set; }
        public string Name { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string Role { get; set; } // "Owner" or "Member"
    }
}