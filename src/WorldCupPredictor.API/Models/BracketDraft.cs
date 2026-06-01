using System.Text.Json;

namespace WorldCupPredictor.API.Models;

public class BracketDraft
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public BracketTier Tier { get; set; }

    /// <summary>Full bracket picks serialised as JSON (BracketSubmitRequest-shape).</summary>
    public string PicksJson { get; set; } = "{}";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>True when this draft was used as the user's final submission.</summary>
    public bool IsSubmittedFinal { get; set; } = false;

    public User User { get; set; } = null!;
}
