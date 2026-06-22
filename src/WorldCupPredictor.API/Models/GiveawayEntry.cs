namespace WorldCupPredictor.API.Models;

public class GiveawayEntry
{
    public int Id { get; set; }
    public int GiveawayId { get; set; }
    public int UserId { get; set; }
    public int HomeScore { get; set; }
    public int AwayScore { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public Giveaway Giveaway { get; set; } = null!;
    public User User { get; set; } = null!;
}
