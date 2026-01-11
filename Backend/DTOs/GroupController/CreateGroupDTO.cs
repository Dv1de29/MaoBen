using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs.GroupController
{
    public class CreateGroupDTO
    {
        [Required(ErrorMessage = "Group must have a name!")]
        [MaxLength(100, ErrorMessage = "Group name can not have more than 100 characters!")]
        public required string Name { get; set; }


        [Required(ErrorMessage = "Group must have a description!")]
        [MaxLength(500, ErrorMessage = "Group description can not have more than 500 characters!")]
        public required string Description { get; set; }
    }
}
