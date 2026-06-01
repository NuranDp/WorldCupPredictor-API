namespace WorldCupPredictor.API.Models;

public class TournamentConfig
{
    public int Id { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime BracketLockDate { get; set; }
    public bool IsActive { get; set; }
    public string Season { get; set; } = string.Empty;
}
