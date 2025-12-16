namespace Backend.DTOs.AuthController
{
    public class AuthResponseDTO
    {
        public required string Token { get; set; }
        public required string Username { get; set; }
        public required string ProfilePictureUrl { get; set; }
        public required string Role { get; set; }
    }
}
