using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs.GroupController
{
    public class SendGroupMessageDTO
    {
        [Required(ErrorMessage =" Message content can not be empty!")]
        [MaxLength(500, ErrorMessage = "Message content can not have more than 500 characters!")]
        public required string Content { get; set; }
    }
}
