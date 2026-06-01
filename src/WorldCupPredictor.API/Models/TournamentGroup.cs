namespace WorldCupPredictor.API.Models;

public class TournamentGroup
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // A–L

    /// <summary>Actual group winner (set by admin / results poller)</summary>
    public int? ActualFirstTeamId { get; set; }
    /// <summary>Actual group runner-up (set by admin / results poller)</summary>
    public int? ActualSecondTeamId { get; set; }

    public Team? ActualFirstTeam { get; set; }
    public Team? ActualSecondTeam { get; set; }
    public ICollection<Team> Teams { get; set; } = new List<Team>();
}
