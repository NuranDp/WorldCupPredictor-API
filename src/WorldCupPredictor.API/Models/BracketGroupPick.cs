namespace WorldCupPredictor.API.Models;

public class BracketGroupPick
{
    public int Id { get; set; }
    public int BracketId { get; set; }
    public int GroupId { get; set; }
    public int? FirstTeamId { get; set; }
    public int? SecondTeamId { get; set; }

    public Bracket Bracket { get; set; } = null!;
    public TournamentGroup Group { get; set; } = null!;
    public Team? FirstTeam { get; set; }
    public Team? SecondTeam { get; set; }
}
