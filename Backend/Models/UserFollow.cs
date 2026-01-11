using Backend.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class UserFollow
{
    [Required(ErrorMessage = "Follow relationship must have a source user.")]
    public required string SourceUserId { get; set; }

    [ForeignKey("SourceUserId")]
    public virtual ApplicationUser SourceUser { get; set; } = default!;

    [Required(ErrorMessage = "Follow relationship must have a target user.")]
    public required string TargetUserId { get; set; }

    [ForeignKey("TargetUserId")]
    public virtual ApplicationUser TargetUser { get; set; } = default!;

    public FollowStatus Status { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum FollowStatus
{
    Pending,
    Accepted
}