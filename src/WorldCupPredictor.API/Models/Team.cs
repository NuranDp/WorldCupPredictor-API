using System.ComponentModel.DataAnnotations.Schema;

namespace WorldCupPredictor.API.Models;

public class Team
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    [Column("FlagEmoji")]          // DB column name unchanged — no migration needed
    public string FlagUrl { get; set; } = string.Empty;

    public string FifaCode { get; set; } = string.Empty;
    public int GroupId { get; set; }
    public int Seeding { get; set; }
    public int FifaRanking { get; set; }

    public TournamentGroup Group { get; set; } = null!;
}
