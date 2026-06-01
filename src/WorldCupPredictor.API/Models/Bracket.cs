namespace WorldCupPredictor.API.Models;

public class Bracket
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public bool IsLocked { get; set; }
    public int TotalPoints { get; set; }
    public BracketTier Tier { get; set; } = BracketTier.Bronze;

    public User User { get; set; } = null!;
    public ICollection<BracketPick> Picks { get; set; } = new List<BracketPick>();
    public ICollection<BracketGroupPick> GroupPicks { get; set; } = new List<BracketGroupPick>();
    public ICollection<BracketBest3rdPick> Best3rdPicks { get; set; } = new List<BracketBest3rdPick>();
}
