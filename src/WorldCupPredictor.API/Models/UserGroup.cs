namespace WorldCupPredictor.API.Models;

public class UserGroup
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int OwnerId { get; set; }
    public string InviteCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User Owner { get; set; } = null!;
    public ICollection<UserGroupMember> Members { get; set; } = new List<UserGroupMember>();
}
