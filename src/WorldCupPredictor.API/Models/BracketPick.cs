namespace WorldCupPredictor.API.Models;

public class BracketPick
{
    public int Id { get; set; }
    public int BracketId { get; set; }
    public int MatchId { get; set; }
    public int? PickedTeamId { get; set; }

    /// <summary>Predicted home team score (null = not entered)</summary>
    public int? HomeScore { get; set; }

    /// <summary>Predicted away team score (null = not entered)</summary>
    public int? AwayScore { get; set; }

    public int PointsEarned { get; set; }
    public bool IsCorrect { get; set; }

    public Bracket Bracket { get; set; } = null!;
    public Match Match { get; set; } = null!;
    public Team? PickedTeam { get; set; }
    public ICollection<BracketPickLineupPlayer> LineupPlayers { get; set; } = new List<BracketPickLineupPlayer>();
}
