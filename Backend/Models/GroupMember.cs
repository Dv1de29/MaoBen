using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
{
    public class GroupMember
    {
        [Required(ErrorMessage = "Member must be assigned to a group.")]
        public int GroupId { get; set; }

        [ForeignKey("GroupId")]
        public virtual Group Group { get; set; } = default!;

        [Required(ErrorMessage = "Member must be assigned to a user.")]
        public required string UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = default!;

        public GroupMemberStatus Status { get; set; } 

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }

    public enum GroupMemberStatus
    {
        Pending,
        Accepted
    }
}