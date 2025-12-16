namespace Backend.Models
{
    public class PostLike
    {
        public int PostId { get; set; }
        public Posts Post { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
    }
}