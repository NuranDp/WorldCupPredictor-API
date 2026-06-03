using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorldCupPredictor.API.Data;
using WorldCupPredictor.API.DTOs;
using WorldCupPredictor.API.Models;

namespace WorldCupPredictor.API.Controllers;

[ApiController]
[Route("api/leaderboard")]
public class LeaderboardController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetLeaderboard([FromQuery] string? tier = null)
    {
        var query = db.Brackets.Include(b => b.User).AsQueryable();

        if (!string.IsNullOrWhiteSpace(tier) && Enum.TryParse<BracketTier>(tier, ignoreCase: true, out var parsedTier))
            query = query.Where(b => b.Tier == parsedTier);

        var entries = await query
            .OrderByDescending(b => b.TotalPoints)
            .ThenBy(b => b.SubmittedAt)
            .Select(b => new
            {
                b.UserId,
                b.User.Name,
                b.User.AvatarUrl,
                b.TotalPoints,
                b.SubmittedAt,
                Tier = b.Tier.ToString(),
                b.ShareToken,
            })
            .ToListAsync();

        var result = entries.Select((e, i) => new LeaderboardEntryDto(
            Rank: i + 1,
            UserId: e.UserId,
            Name: e.Name,
            AvatarUrl: e.AvatarUrl,
            TotalPoints: e.TotalPoints,
            SubmittedAt: e.SubmittedAt,
            ShareToken: e.ShareToken
        )).ToList();

        return Ok(result);
    }
}
