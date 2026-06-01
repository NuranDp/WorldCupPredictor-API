namespace WorldCupPredictor.API.Models;

public class UserGroupMember
{
    public int UserGroupId { get; set; }
    public int UserId { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public UserGroup UserGroup { get; set; } = null!;
    public User User { get; set; } = null!;
}
