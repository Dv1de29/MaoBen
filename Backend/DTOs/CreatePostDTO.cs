namespace Backend.DTOs
{
    public class CreatePostDTO
    {
        public IFormFile Image { get; set; }

        public string Description { get; set; }
    }
}
