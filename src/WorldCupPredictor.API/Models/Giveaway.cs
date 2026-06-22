namespace WorldCupPredictor.API.Models;

public class Giveaway
{
    public int Id { get; set; }
    public int MatchId { get; set; }
    public string Prize { get; set; } = string.Empty;
    public GiveawayStatus Status { get; set; } = GiveawayStatus.Open;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? WinnerUserId { get; set; }
    public DateTime? DrawnAt { get; set; }
    public bool IsLuckyDraw { get; set; }

    public Match Match { get; set; } = null!;
    public User? Winner { get; set; }
    public ICollection<GiveawayEntry> Entries { get; set; } = new List<GiveawayEntry>();
}

public enum GiveawayStatus { Open, Closed, Drawn }
