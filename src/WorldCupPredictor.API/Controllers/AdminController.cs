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
    public async Task<IActionResult> SyncResults([FromQuery] int days = 3)
    {
        if (!IsAdmin) return Forbid();
        var count = await espn.SyncResultsAsync(days);
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

    // ── Get all group stage matches with kickoff times ────────────────────────
    [HttpGet("group-matches")]
    public async Task<IActionResult> GetGroupStageMatches()
    {
        if (!IsAdmin) return Forbid();

        var matches = await db.Matches
            .Where(m => m.Round == MatchRound.GroupStage)
            .Include(m => m.HomeTeam).ThenInclude(t => t!.Group)
            .Include(m => m.AwayTeam)
            .OrderBy(m => m.MatchDate)
            .Select(m => new
            {
                m.Id,
                GroupName = m.HomeTeam!.Group.Name,
                HomeTeamId = m.HomeTeamId,
                HomeTeamName = m.HomeTeam.Name,
                HomeTeamFlag = m.HomeTeam.FlagUrl,
                AwayTeamId = m.AwayTeamId,
                AwayTeamName = m.AwayTeam!.Name,
                AwayTeamFlag = m.AwayTeam.FlagUrl,
                m.MatchDate,
                Status = m.Status.ToString(),
                m.HomeScore,
                m.AwayScore,
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

        var hasOpen = await db.Giveaways.AnyAsync(g => g.Status != GiveawayStatus.Drawn);
        if (hasOpen) return BadRequest(new { message = "Close or draw the existing giveaway first." });

        var match = await db.Matches.FindAsync(req.MatchId);
        if (match is null) return NotFound(new { message = "Match not found." });

        var giveaway = new Giveaway { MatchId = req.MatchId, Prize = req.Prize };
        db.Giveaways.Add(giveaway);
        await db.SaveChangesAsync();

        return Ok(new { giveaway.Id });
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
    public async Task<IActionResult> DrawGiveaway(int id)
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

        const int MinParticipants = 100;
        int totalEntries = giveaway.Entries.Count;

        if (totalEntries < MinParticipants)
            return BadRequest(new { message = $"Need at least {MinParticipants} entries to draw. Current: {totalEntries}." });

        List<GiveawayEntry> correctPool = giveaway.Match.Status == MatchStatus.Completed
            && giveaway.Match.HomeScore.HasValue
            && giveaway.Match.AwayScore.HasValue
            ? giveaway.Entries
                .Where(e => e.HomeScore == giveaway.Match.HomeScore.Value
                         && e.AwayScore == giveaway.Match.AwayScore.Value)
                .ToList()
            : [];

        bool isLuckyDraw = correctPool.Count == 0;
        var pool = isLuckyDraw ? giveaway.Entries.ToList() : correctPool;

        var winner = pool[Random.Shared.Next(pool.Count)];

        giveaway.WinnerUserId = winner.UserId;
        giveaway.DrawnAt = DateTime.UtcNow;
        giveaway.Status = GiveawayStatus.Drawn;
        giveaway.IsLuckyDraw = isLuckyDraw;

        await db.SaveChangesAsync();

        return Ok(new
        {
            winnerName = winner.User.Name,
            isLuckyDraw,
            message = isLuckyDraw
                ? $"Lucky draw winner: {winner.User.Name} (no correct predictions)"
                : $"Winner: {winner.User.Name} — predicted the exact score! ({correctPool.Count} correct entries)",
        });
    }

    // ── Get current giveaway (admin view with entry count) ────────────────────
    [HttpGet("giveaway")]
    public async Task<IActionResult> GetGiveaway()
    {
        if (!IsAdmin) return Forbid();

        var giveaway = await db.Giveaways
            .Include(g => g.Match).ThenInclude(m => m.HomeTeam)
            .Include(g => g.Match).ThenInclude(m => m.AwayTeam)
            .Include(g => g.Winner)
            .OrderByDescending(g => g.CreatedAt)
            .FirstOrDefaultAsync();

        if (giveaway is null) return Ok(null);

        return Ok(new
        {
            giveaway.Id,
            giveaway.Prize,
            Status = giveaway.Status.ToString(),
            giveaway.IsLuckyDraw,
            EntryCount = await db.GiveawayEntries.CountAsync(e => e.GiveawayId == giveaway.Id),
            Match = new
            {
                giveaway.Match.Id,
                HomeTeam = giveaway.Match.HomeTeam?.Name,
                HomeTeamFlag = giveaway.Match.HomeTeam?.FlagUrl,
                AwayTeam = giveaway.Match.AwayTeam?.Name,
                AwayTeamFlag = giveaway.Match.AwayTeam?.FlagUrl,
                giveaway.Match.MatchDate,
                Status = giveaway.Match.Status.ToString(),
            },
            WinnerName = giveaway.Winner?.Name,
        });
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
public record CreateGiveawayRequest(int MatchId, string Prize);
