using System.ComponentModel.DataAnnotations;

namespace Backend.Models
{
    public class GroupMember
    {
        public int GroupId { get; set; }
        public Group Group { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public GroupMemberStatus Status { get; set; } // Pending / Accepted

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }

    public enum GroupMemberStatus
    {
        Pending,
        Accepted
    }
}