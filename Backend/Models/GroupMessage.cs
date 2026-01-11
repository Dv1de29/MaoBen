using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
{
    public class GroupMessage
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Content of a message can not be empty!")]
        [MaxLength(500,ErrorMessage = "A message can not have more than 500 characters!")]
        public required string Content { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsEdited { get; set; } = false;

        [Required(ErrorMessage = "A message must be assigned to a group!")]
        public int GroupId { get; set; }

        [ForeignKey("GroupId")]
        public virtual Group Group { get; set; } = default!;

        [Required(ErrorMessage = "A message must be assigned to a user!")]
        public required string UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = default!;
    }
}