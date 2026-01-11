using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs.DirectMessageController
{
    public class SendDirectMessageDTO
    {
        [Required(ErrorMessage ="Message can not be empty!")]
        [MaxLength(500, ErrorMessage ="Message can not exceed 500 characters!")]
        public string Content { get; set; }
    }
}
