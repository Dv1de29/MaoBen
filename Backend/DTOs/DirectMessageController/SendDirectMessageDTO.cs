using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs.DirectMessageController
{
    public class SendDirectMessageDTO
    {
        [MaxLength(500, ErrorMessage ="Message can not exceed 500 characters!")]
        public string Content { get; set; }
    }
}
