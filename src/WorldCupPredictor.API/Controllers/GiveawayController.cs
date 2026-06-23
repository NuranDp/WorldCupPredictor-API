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

    // ── Active giveaway (public) ──────────────────────────────────────────────
    [HttpGet("active")]
    [AllowAnonymous]
    public async Task<IActionResult> GetActive()
    {
        var giveaway = await db.Giveaways
            .Include(g => g.Match).ThenInclude(m => m.HomeTeam)
            .Include(g => g.Match).ThenInclude(m => m.AwayTeam)
            .Include(g => g.Winner)
            .FirstOrDefaultAsync(g => g.IsActive);

        if (giveaway is null) return Ok(null);

        return Ok(new
        {
            giveaway.Id,
            giveaway.Prize,
            Status = giveaway.Status.ToString(),
            EntryCount = await db.GiveawayEntries.CountAsync(e => e.GiveawayId == giveaway.Id),
            giveaway.IsLuckyDraw,
            Match = new
            {
                giveaway.Match.Id,
                HomeTeam = giveaway.Match.HomeTeam?.Name,
                HomeTeamFlag = giveaway.Match.HomeTeam?.FlagUrl,
                AwayTeam = giveaway.Match.AwayTeam?.Name,
                AwayTeamFlag = giveaway.Match.AwayTeam?.FlagUrl,
                giveaway.Match.MatchDate,
                HomeScore = giveaway.Match.HomeScore,
                AwayScore = giveaway.Match.AwayScore,
            },
            Winner = giveaway.Winner is null ? null : new
            {
                giveaway.Winner.Name,
                giveaway.Winner.AvatarUrl,
                DrawnAt = giveaway.DrawnAt,
            },
        });
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
