namespace WorldCupPredictor.API.Models;

public class Match
{
    public int Id { get; set; }
    public MatchRound Round { get; set; }
    public int? SlotNumber { get; set; }    // 1–32 for knockout bracket position
    public int? HomeTeamId { get; set; }
    public int? AwayTeamId { get; set; }
    public int? HomeScore { get; set; }
    public int? AwayScore { get; set; }
    public DateTime? MatchDate { get; set; }
    public MatchStatus Status { get; set; } = MatchStatus.Scheduled;
    public string? ExternalApiId { get; set; }

    public int? WinnerTeamId { get; set; }   // set after match is Completed

    public Team? HomeTeam { get; set; }
    public Team? AwayTeam { get; set; }
    public Team? WinnerTeam { get; set; }
    public ICollection<BracketPick> BracketPicks { get; set; } = new List<BracketPick>();
}

public enum MatchRound
{
    GroupStage,
    RoundOf32,
    RoundOf16,
    QuarterFinal,
    SemiFinal,
    ThirdPlace,
    Final
}

public enum MatchStatus
{
    Scheduled,
    InProgress,
    Completed,
    Cancelled
}
