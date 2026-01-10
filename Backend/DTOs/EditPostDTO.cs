namespace Backend.DTOs
{
    public class EditPostDto
    {
        public string? Description { get; set; }
        public IFormFile? Image { get; set; }
    }
}