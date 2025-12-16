using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
{
    public class Comment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(500)]
        public string Content { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Relatia cu User (Cine a scris comentariul)
        [ForeignKey("User")]
        public string UserId { get; set; } // FK Explicit
        public ApplicationUser User { get; set; } = null!;

        // Relatia cu Post (La ce postare)
        public int PostId { get; set; }
        [ForeignKey("PostId")]
        public Posts Post { get; set; } // Fara punct si virgula in plus
    }
}