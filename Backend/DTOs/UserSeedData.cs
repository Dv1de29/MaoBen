namespace Backend.DTOs
{
    // 1. Remove the outer 'class UserSeedData' definition.
    // 2. Define the record directly in the namespace.
    public record UserSeedData(string FirstName, string LastName, string Username, string Email, string Password);
}