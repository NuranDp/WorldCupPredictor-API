namespace WorldCupPredictor.API.Models;

public class Player
{
    public int Id { get; set; }
    public int TeamId { get; set; }

    /// <summary>Full player name</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>GK | DEF | MID | FWD</summary>
    public string Position { get; set; } = string.Empty;

    public int ShirtNumber { get; set; }

    public Team Team { get; set; } = null!;
}
