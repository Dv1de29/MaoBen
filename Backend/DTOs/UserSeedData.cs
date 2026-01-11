namespace Backend.DTOs
{
    public record UserSeedData(string FirstName, string LastName, string Username, string Email, string Password, string ProfilePictureUrl = "/assets/img/no_user.png");
}