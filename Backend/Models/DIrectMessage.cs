using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
{
    /// <summary>
    /// Reprezentează un mesaj direct între doi utilizatori
    /// - Nu este necesar ca utilizatorii să se urmărească
    /// - Ambii utilizatori pot vedea conversația
    /// - Doar autorul poate edita/șterge mesajul
    /// </summary>
    public class DirectMessage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // --- Foreign Keys & Relationships ---

        /// <summary>
        /// ID-ul utilizatorului care a trimis mesajul
        /// </summary>
        [ForeignKey("Sender")]
        public string SenderId { get; set; } = string.Empty;

        /// <summary>
        /// Referință la utilizatorul care a trimis mesajul
        /// </summary>
        public ApplicationUser? Sender { get; set; }

        /// <summary>
        /// ID-ul utilizatorului care primește mesajul
        /// </summary>
        [ForeignKey("Receiver")]
        public string ReceiverId { get; set; } = string.Empty;

        /// <summary>
        /// Referință la utilizatorul care primește mesajul
        /// </summary>
        public ApplicationUser? Receiver { get; set; }
    }
}
