namespace Backend.Models
{
    public class Posts
    {
        public int Id { get; set; }

        public string OwnerID { get; set; }

        public int Nr_likes { get; set; }

        public int Nr_Comms { get; set; }

        public string Image_path { get; set; }

        public string Description { get; set; }

        public ApplicationUser User { get; set; }
    }
}
