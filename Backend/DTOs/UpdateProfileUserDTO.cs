namespace Backend.DTOs
{
    public class UpdateProfileUserDTO
    {
        public bool Privacy { get; set; }
        public string Description { get; set; }

        public string ProfilePictureUrl { get; set; }
    }
}
