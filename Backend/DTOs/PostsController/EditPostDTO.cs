namespace Backend.DTOs.PostsController
{
    public class EditPostDto
    {
        public string? Description { get; set; }
        public IFormFile? Image { get; set; }
    }
}