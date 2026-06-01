namespace WorldCupPredictor.API.Models;

/// <summary>
/// Records which teams actually qualified as one of the 8 best 3rd-place
/// teams in the tournament. Populated by the admin or results poller.
/// </summary>
public class ActualBest3rdQualifier
{
    public int Id { get; set; }
    public int TeamId { get; set; }
    public Team Team { get; set; } = null!;
}
