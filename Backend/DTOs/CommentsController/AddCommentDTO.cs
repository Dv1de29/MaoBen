using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs.CommentsController
{
    public class AddCommentDto
    {
        [Required(ErrorMessage = "The Post ID is required to associate this comment.")]
        public int PostId { get; set; }

        [Required(ErrorMessage = "Comment content is required and cannot be empty.")]
        [MaxLength(500, ErrorMessage = "The comment is too long. Please limit your comment to 500 characters.")]
        public required string Content { get; set; }
    }
}