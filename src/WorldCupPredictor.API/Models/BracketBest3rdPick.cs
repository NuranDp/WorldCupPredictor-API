namespace WorldCupPredictor.API.Models;

/// <summary>
/// One of the 8 best-3rd-place qualifier picks for a bracket.
/// Rank 1-8 determines pairing: 1v2 → R32 slot 13, 3v4 → slot 14, 5v6 → slot 15, 7v8 → slot 16.
/// </summary>
public class BracketBest3rdPick
{
    public int Id { get; set; }
    public int BracketId { get; set; }

    /// <summary>Position 1-8. Pairing: odd = home, even = away of that match.</summary>
    public int Rank { get; set; }

    public int? TeamId { get; set; }

    public Bracket Bracket { get; set; } = null!;
    public Team? Team { get; set; }
}
