using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
{
    public class Group
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Group must have a name!")]
        [MaxLength(100)]
        public required string Name { get; set; }

        [Required(ErrorMessage = "Group must have a description!")]
        [MaxLength(500)]
        public required string Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required(ErrorMessage = "Group must have an owner.")]
        public required string OwnerId { get; set; }

        [ForeignKey("OwnerId")]
        public virtual ApplicationUser Owner { get; set; } = default!;
    }
}