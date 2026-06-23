using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorldCupPredictor.API.Data;
using WorldCupPredictor.API.Models;
using WorldCupPredictor.API.Services;

namespace WorldCupPredictor.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize]
public class AdminController(AppDbContext db, ScoringService scoring, ApiFootballService apiFootball, EspnSoccerService espn)
    : ControllerBase
{
    private bool IsAdmin =>
        db.Users.Find(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")!))?.IsAdmin ?? false;

    // ── Recalculate all scores ────────────────────────────────────────────────
    [HttpPost("recalculate")]
    public async Task<IActionResult> Recalculate()
    {
        if (!IsAdmin) return Forbid();
        await scoring.RecalculateAllAsync();
        return Ok(new { message = "Scores recalculated." });
    }

    // ── Trigger ESPN sync (free, no key) ─────────────────────────────────────
    [HttpPost("sync-results")]
    public async Task<IActionResult> SyncResults()
    {
        if (!IsAdmin) return Forbid();
        var count = await espn.SyncResultsAsync();
        return Ok(new { updated = count });
    }

    // ── Trigger API-Football sync (paid plan needed for 2026) ─────────────────
    [HttpPost("sync-results-apifootball")]
    public async Task<IActionResult> SyncResultsApiFootball()
    {
        if (!IsAdmin) return Forbid();
        var count = await apiFootball.SyncResultsAsync();
        return Ok(new { updated = count });
    }

    // ── Update a knockout match result manually ───────────────────────────────
    [HttpPut("match/{matchId:int}/result")]
    public async Task<IActionResult> SetMatchResult(int matchId, [FromBody] MatchResultRequest req)
    {
        if (!IsAdmin) return Forbid();

        var match = await db.Matches.FindAsync(matchId);
        if (match is null) return NotFound();

        match.HomeScore = req.HomeScore;
        match.AwayScore = req.AwayScore;
        match.WinnerTeamId = req.WinnerTeamId;
        match.Status = MatchStatus.Completed;

        await db.SaveChangesAsync();
        await scoring.RecalculateAllAsync();

        return Ok(new { message = "Result saved and scores updated." });
    }

    // ── Set actual group standings ────────────────────────────────────────────
    [HttpPut("group/{groupId:int}/standings")]
    public async Task<IActionResult> SetGroupStandings(int groupId, [FromBody] GroupStandingsRequest req)
    {
        if (!IsAdmin) return Forbid();

        var group = await db.TournamentGroups.FindAsync(groupId);
        if (group is null) return NotFound();

        group.ActualFirstTeamId = req.FirstTeamId;
        group.ActualSecondTeamId = req.SecondTeamId;

        await db.SaveChangesAsync();
        await scoring.RecalculateAllAsync();

        return Ok(new { message = "Group standings saved and scores updated." });
    }

    // ── Set actual best-3rd qualifiers ────────────────────────────────────────
    [HttpPut("best3rd")]
    public async Task<IActionResult> SetBest3rdQualifiers([FromBody] Best3rdQualifiersRequest req)
    {
        if (!IsAdmin) return Forbid();

        // Replace the full list
        var existing = await db.ActualBest3rdQualifiers.ToListAsync();
        db.ActualBest3rdQualifiers.RemoveRange(existing);

        foreach (var teamId in req.TeamIds.Distinct().Take(8))
        {
            db.ActualBest3rdQualifiers.Add(new ActualBest3rdQualifier { TeamId = teamId });
        }

        await db.SaveChangesAsync();
        await scoring.RecalculateAllAsync();

        return Ok(new { message = "Best 3rd qualifiers saved and scores updated." });
    }

    // ── Lock all brackets ─────────────────────────────────────────────────────
    [HttpPost("lock-brackets")]
    public async Task<IActionResult> LockBrackets()
    {
        if (!IsAdmin) return Forbid();
        await db.Brackets.ExecuteUpdateAsync(s => s.SetProperty(b => b.IsLocked, true));
        return Ok(new { message = "All brackets locked." });
    }

    // ── Get all knockout matches with full detail ─────────────────────────────
    [HttpGet("matches")]
    public async Task<IActionResult> GetAdminMatches()
    {
        if (!IsAdmin) return Forbid();

        var matches = await db.Matches
            .Where(m => m.Round != MatchRound.GroupStage)
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .OrderBy(m => m.Round)
            .ThenBy(m => m.SlotNumber)
            .Select(m => new
            {
                m.Id,
                m.SlotNumber,
                Round = m.Round.ToString(),
                HomeTeamId = m.HomeTeamId,
                HomeTeamName = m.HomeTeam != null ? m.HomeTeam.Name : null,
                AwayTeamId = m.AwayTeamId,
                AwayTeamName = m.AwayTeam != null ? m.AwayTeam.Name : null,
                WinnerTeamId = m.WinnerTeamId,
                m.HomeScore,
                m.AwayScore,
                Status = m.Status.ToString(),
            })
            .ToListAsync();

        return Ok(matches);
    }

    // ── Get all groups with actual standings ──────────────────────────────────
    [HttpGet("groups")]
    public async Task<IActionResult> GetAdminGroups()
    {
        if (!IsAdmin) return Forbid();

        var groups = await db.TournamentGroups
            .Include(g => g.Teams)
            .Include(g => g.ActualFirstTeam)
            .Include(g => g.ActualSecondTeam)
            .OrderBy(g => g.Name)
            .Select(g => new
            {
                g.Id,
                g.Name,
                ActualFirstTeamId = g.ActualFirstTeamId,
                ActualSecondTeamId = g.ActualSecondTeamId,
                Teams = g.Teams.Select(t => new { t.Id, t.Name, t.FlagUrl }).ToList(),
            })
            .ToListAsync();

        return Ok(groups);
    }

    // ── Get actual best 3rd qualifiers ────────────────────────────────────────
    [HttpGet("best3rd")]
    public async Task<IActionResult> GetBest3rdQualifiers()
    {
        if (!IsAdmin) return Forbid();

        var qualifiers = await db.ActualBest3rdQualifiers
            .Select(q => q.TeamId)
            .ToListAsync();

        return Ok(new { teamIds = qualifiers });
    }

    // ── Create a giveaway ─────────────────────────────────────────────────────
    [HttpPost("giveaway")]
    public async Task<IActionResult> CreateGiveaway([FromBody] CreateGiveawayRequest req)
    {
        if (!IsAdmin) return Forbid();

        var match = await db.Matches.FindAsync(req.MatchId);
        if (match is null) return NotFound(new { message = "Match not found." });

        var hasActive = await db.Giveaways.AnyAsync(g => g.IsActive);
        var giveaway = new Giveaway
        {
            MatchId = req.MatchId,
            Prize = req.Prize,
            IsActive = !hasActive, // auto-activate only when nothing else is active
        };
        db.Giveaways.Add(giveaway);
        await db.SaveChangesAsync();

        return Ok(new { giveaway.Id });
    }

    // ── Set a giveaway as the publicly active one ─────────────────────────────
    [HttpPost("giveaway/{id:int}/activate")]
    public async Task<IActionResult> ActivateGiveaway(int id)
    {
        if (!IsAdmin) return Forbid();

        var giveaway = await db.Giveaways.FindAsync(id);
        if (giveaway is null) return NotFound();
        if (giveaway.Status == GiveawayStatus.Drawn)
            return BadRequest(new { message = "Cannot activate a completed draw." });

        await db.Giveaways
            .Where(g => g.IsActive && g.Id != id)
            .ExecuteUpdateAsync(s => s.SetProperty(g => g.IsActive, false));

        giveaway.IsActive = true;
        await db.SaveChangesAsync();

        return Ok(new { message = "Giveaway is now active." });
    }

    // ── Close entries ─────────────────────────────────────────────────────────
    [HttpPost("giveaway/{id:int}/close")]
    public async Task<IActionResult> CloseGiveaway(int id)
    {
        if (!IsAdmin) return Forbid();

        var giveaway = await db.Giveaways.FindAsync(id);
        if (giveaway is null) return NotFound();
        if (giveaway.Status != GiveawayStatus.Open)
            return BadRequest(new { message = "Giveaway is not open." });

        giveaway.Status = GiveawayStatus.Closed;
        await db.SaveChangesAsync();

        return Ok(new { message = "Entries closed." });
    }

    // ── Draw winner ───────────────────────────────────────────────────────────
    [HttpPost("giveaway/{id:int}/draw")]
    public async Task<IActionResult> DrawGiveaway(int id, [FromQuery] bool lucky = false)
    {
        if (!IsAdmin) return Forbid();

        var giveaway = await db.Giveaways
            .Include(g => g.Match)
            .Include(g => g.Entries).ThenInclude(e => e.User)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (giveaway is null) return NotFound();
        if (giveaway.Status == GiveawayStatus.Drawn)
            return BadRequest(new { message = "Winner already drawn." });
        if (!giveaway.Entries.Any())
            return BadRequest(new { message = "No entries to draw from." });

        if (lucky)
        {
            // Lucky draw: pick from ALL entries regardless of correct predictions
            var pool = giveaway.Entries.ToList();
            var winner = pool[Random.Shared.Next(pool.Count)];

            giveaway.WinnerUserId = winner.UserId;
            giveaway.DrawnAt = DateTime.UtcNow;
            giveaway.Status = GiveawayStatus.Drawn;
            giveaway.IsLuckyDraw = true;

            await db.SaveChangesAsync();

            return Ok(new
            {
                winnerName = winner.User.Name,
                isLuckyDraw = true,
                message = $"Lucky draw winner: {winner.User.Name}!",
            });
        }

        // Regular draw: correct predictions only
        if (giveaway.Match.Status != MatchStatus.Completed)
            return BadRequest(new { message = "Match result must be synced before drawing from correct predictions." });

        List<GiveawayEntry> correctPool = giveaway.Match.HomeScore.HasValue && giveaway.Match.AwayScore.HasValue
            ? giveaway.Entries
                .Where(e => e.HomeScore == giveaway.Match.HomeScore.Value
                         && e.AwayScore == giveaway.Match.AwayScore.Value)
                .ToList()
            : [];

        if (correctPool.Count == 0)
            return BadRequest(new { message = "No correct predictions found. Use Lucky Draw to pick from all entries." });

        var regularWinner = correctPool[Random.Shared.Next(correctPool.Count)];

        giveaway.WinnerUserId = regularWinner.UserId;
        giveaway.DrawnAt = DateTime.UtcNow;
        giveaway.Status = GiveawayStatus.Drawn;
        giveaway.IsLuckyDraw = false;

        await db.SaveChangesAsync();

        return Ok(new
        {
            winnerName = regularWinner.User.Name,
            isLuckyDraw = false,
            message = $"Winner: {regularWinner.User.Name} — predicted the exact score! ({correctPool.Count} correct entries)",
        });
    }

    // ── Get all giveaways (admin view) ───────────────────────────────────────
    [HttpGet("giveaway")]
    public async Task<IActionResult> GetGiveaway()
    {
        if (!IsAdmin) return Forbid();

        var giveaways = await db.Giveaways
            .Include(g => g.Match).ThenInclude(m => m.HomeTeam)
            .Include(g => g.Match).ThenInclude(m => m.AwayTeam)
            .Include(g => g.Winner)
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
            g.IsLuckyDraw,
            g.IsActive,
            g.CreatedAt,
            EntryCount = entryCounts.GetValueOrDefault(g.Id, 0),
            Match = new
            {
                g.Match.Id,
                HomeTeam = g.Match.HomeTeam?.Name,
                HomeTeamFlag = g.Match.HomeTeam?.FlagUrl,
                AwayTeam = g.Match.AwayTeam?.Name,
                AwayTeamFlag = g.Match.AwayTeam?.FlagUrl,
                g.Match.MatchDate,
                Status = g.Match.Status.ToString(),
            },
            WinnerName = g.Winner?.Name,
        }));
    }

    // ── Get all entries for a giveaway ───────────────────────────────────────
    [HttpGet("giveaway/{id:int}/entries")]
    public async Task<IActionResult> GetGiveawayEntries(int id)
    {
        if (!IsAdmin) return Forbid();

        var giveaway = await db.Giveaways
            .Include(g => g.Match)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (giveaway is null) return NotFound();

        var entries = await db.GiveawayEntries
            .Include(e => e.User)
            .Where(e => e.GiveawayId == id)
            .OrderBy(e => e.SubmittedAt)
            .Select(e => new
            {
                e.Id,
                UserName = e.User.Name,
                e.HomeScore,
                e.AwayScore,
                SubmittedAt = e.SubmittedAt,
                IsCorrect = giveaway.Match.Status == MatchStatus.Completed
                    && giveaway.Match.HomeScore.HasValue
                    && giveaway.Match.AwayScore.HasValue
                    && e.HomeScore == giveaway.Match.HomeScore.Value
                    && e.AwayScore == giveaway.Match.AwayScore.Value,
            })
            .ToListAsync();

        return Ok(entries);
    }

    // ── Delete giveaway ───────────────────────────────────────────────────────
    [HttpDelete("giveaway/{id:int}")]
    public async Task<IActionResult> DeleteGiveaway(int id)
    {
        if (!IsAdmin) return Forbid();

        var giveaway = await db.Giveaways.FindAsync(id);
        if (giveaway is null) return NotFound();

        db.Giveaways.Remove(giveaway);
        await db.SaveChangesAsync();

        return Ok(new { message = "Giveaway deleted." });
    }
}

public record MatchResultRequest(int? HomeScore, int? AwayScore, int? WinnerTeamId);
public record GroupStandingsRequest(int? FirstTeamId, int? SecondTeamId);
public record Best3rdQualifiersRequest(List<int> TeamIds);
