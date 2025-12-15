using Backend.Models;

public class UserFollow
{
    // Cine dă follow (Urmăritorul)
    public string SourceUserId { get; set; }
    public ApplicationUser SourceUser { get; set; }

    // Cine primește follow (Urmăritul)
    public string TargetUserId { get; set; }
    public ApplicationUser TargetUser { get; set; }

    // Statusul cererii: 0 = Pending, 1 = Accepted
    public FollowStatus Status { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum FollowStatus
{
    Pending,
    Accepted
}