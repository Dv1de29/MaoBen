namespace Backend.DTOs
{
    public class AddCommentDto
    {
        public int PostId { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}
