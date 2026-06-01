namespace WorldCupPredictor.API.Models;

public class BracketPickLineupPlayer
{
    public int Id { get; set; }
    public int BracketPickId { get; set; }
    public int PlayerId { get; set; }
    public BracketPick BracketPick { get; set; } = null!;
    public Player Player { get; set; } = null!;
}
