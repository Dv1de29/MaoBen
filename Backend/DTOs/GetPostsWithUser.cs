namespace Backend.DTOs
{
    public class GetPostsWithUser
    {
        public int Id { get; set; }

        public string OwnerID { get; set; }

        public int Nr_likes { get; set; }

        public int Nr_Comms { get; set; }

        public string Image_path { get; set; }

        public string Description { get; set; }

        public DateTime Created { get; set; }

        public string Username  { get; set; }

        public string user_iamge_path { get; set; }
    }
}
