namespace Backend.DTOs.ProfileController
{
    public class GetAllProfilesDTO
    {
        public required string username { get; set; }

        public required string name { get; set; }

        public string ProfilePictureUrl { get; set; } = "/assets/img/no_user.png";
    }
}
