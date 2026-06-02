namespace WorldCupPredictor.API.Models;

public class RefreshToken
{
    public int Id { get; set; }
    public string Token { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime Expiry { get; set; }
    public bool IsRevoked { get; set; }
}
