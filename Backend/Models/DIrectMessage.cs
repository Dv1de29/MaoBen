using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
{
    public class DirectMessage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(500)]
        public required string Content { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


        [Required(ErrorMessage = "Message must be associated with a sender.")]
        public required string SenderId { get; set; }

        [ForeignKey("SenderId")]
        public virtual ApplicationUser Sender { get; set; } = default!;

        [Required(ErrorMessage = "Message must be associated with a receiver.")]
        public required string ReceiverId { get; set; }

        [ForeignKey("ReceiverId")]
        public virtual ApplicationUser Receiver { get; set; } = default!;
    }
}
