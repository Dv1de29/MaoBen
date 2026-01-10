using Microsoft.Extensions.Hosting;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
{
    public class Comment
    {
        [Key]
        public int Id { get; set; }

        
        [Required(ErrorMessage = "Comment content is required and cannot be empty.")]
        [MaxLength(500, ErrorMessage = "The comment is too long. Please limit your comment to 500 characters.")]
        public required string Content { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required(ErrorMessage = "The comment must be associated with an owner.")]
        public required string UserId { get; set; }

        [Required(ErrorMessage = "The comment must be associated with a post.")]
        public int PostId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = default!;

        [ForeignKey("PostId")]
        public virtual Posts Post { get; set; } = default!;
    }
}