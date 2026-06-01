namespace WorldCupPredictor.API.Models;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public string? GoogleId { get; set; }
    public string? AvatarUrl { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsAdmin { get; set; }

    public Bracket? Bracket { get; set; }
    public ICollection<UserGroupMember> UserGroupMemberships { get; set; } = new List<UserGroupMember>();
    public ICollection<UserGroup> OwnedGroups { get; set; } = new List<UserGroup>();
}
