namespace Backend.DTOs.GroupController
{
    public class ShowGroupResponseDTO
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public required string OwnerUsername { get; set; }
        public bool IsUserMember { get; set; }
    }
}
