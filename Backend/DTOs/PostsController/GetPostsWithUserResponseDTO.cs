namespace Backend.DTOs.PostsController
{
    public class GetPostsWithUserResponseDTO
    {
        public int Id { get; set; }

        public required string OwnerID { get; set; }

        public int Nr_likes { get; set; }

        public bool Has_liked { get; set; }

        public int Nr_Comms { get; set; }

        public required string Image_path { get; set; }

        public required string Description { get; set; }

        public DateTime Created { get; set; }

        public required string Username  { get; set; }

        public required string user_image_path { get; set; }
    }
}
