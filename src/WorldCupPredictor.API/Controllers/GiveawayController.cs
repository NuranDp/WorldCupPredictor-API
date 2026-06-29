using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorldCupPredictor.API.Data;
using WorldCupPredictor.API.Models;

namespace WorldCupPredictor.API.Controllers;

[ApiController]
[Route("api/giveaway")]
public class GiveawayController(AppDbContext db) : ControllerBase
{
    private int? CurrentUserId
    {
        get
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            return raw is not null && int.TryParse(raw, out var id) ? id : null;
        }
    }

    // ── All active giveaways (public) ────────────────────────────────────────
    [HttpGet("active")]
    [AllowAnonymous]
    public async Task<IActionResult> GetActive()
    {
        var giveaways = await db.Giveaways
            .Include(g => g.Match).ThenInclude(m => m.HomeTeam)
            .Include(g => g.Match).ThenInclude(m => m.AwayTeam)
            .Include(g => g.Winner)
            .Where(g => g.IsActive && g.Status != GiveawayStatus.Drawn)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();

        if (giveaways.Count == 0) return Ok(Array.Empty<object>());

        var ids = giveaways.Select(g => g.Id).ToList();
        var entryCounts = await db.GiveawayEntries
            .Where(e => ids.Contains(e.GiveawayId))
            .GroupBy(e => e.GiveawayId)
            .Select(grp => new { GiveawayId = grp.Key, Count = grp.Count() })
            .ToDictionaryAsync(x => x.GiveawayId, x => x.Count);

        return Ok(giveaways.Select(g => new
        {
            g.Id,
            g.Prize,
            Status = g.Status.ToString(),
            EntryCount = entryCounts.GetValueOrDefault(g.Id, 0),
            g.IsLuckyDraw,
            Match = new
            {
                g.Match.Id,
                SlotNumber = g.Match.SlotNumber,
                HomeTeam = g.Match.HomeTeam?.Name,
                HomeTeamFlag = g.Match.HomeTeam?.FlagUrl,
                AwayTeam = g.Match.AwayTeam?.Name,
                AwayTeamFlag = g.Match.AwayTeam?.FlagUrl,
                g.Match.MatchDate,
                HomeScore = g.Match.HomeScore,
                AwayScore = g.Match.AwayScore,
            },
            Winner = g.Winner is null ? null : new
            {
                g.Winner.Name,
                g.Winner.AvatarUrl,
                DrawnAt = g.DrawnAt,
            },
        }));
    }

    // ── Submit entry (auth required) ──────────────────────────────────────────
    [HttpPost("{id:int}/enter")]
    [Authorize]
    public async Task<IActionResult> Enter(int id, [FromBody] GiveawayEnterRequest req)
    {
        var userId = CurrentUserId;
        if (userId is null) return Unauthorized();

        var giveaway = await db.Giveaways
            .Include(g => g.Match)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (giveaway is null) return NotFound();
        if (giveaway.Status != GiveawayStatus.Open)
            return BadRequest(new { message = "Entries are closed for this giveaway." });
        if (giveaway.Match.Status == MatchStatus.Completed)
            return BadRequest(new { message = "Match has already been played." });

        var alreadyEntered = await db.GiveawayEntries
            .AnyAsync(e => e.GiveawayId == id && e.UserId == userId.Value);
        if (alreadyEntered)
            return BadRequest(new { message = "You have already entered this giveaway." });

        db.GiveawayEntries.Add(new GiveawayEntry
        {
            GiveawayId = id,
            UserId = userId.Value,
            HomeScore = req.HomeScore,
            AwayScore = req.AwayScore,
        });

        await db.SaveChangesAsync();
        return Ok(new { message = "Entry submitted!" });
    }

    // ── Get current user's entry ──────────────────────────────────────────────
    [HttpGet("{id:int}/my-entry")]
    [Authorize]
    public async Task<IActionResult> GetMyEntry(int id)
    {
        var userId = CurrentUserId;
        if (userId is null) return Unauthorized();

        var entry = await db.GiveawayEntries
            .FirstOrDefaultAsync(e => e.GiveawayId == id && e.UserId == userId.Value);

        if (entry is null) return Ok(null);

        return Ok(new
        {
            entry.HomeScore,
            entry.AwayScore,
            entry.SubmittedAt,
        });
    }
}

public record GiveawayEnterRequest(int HomeScore, int AwayScore);
