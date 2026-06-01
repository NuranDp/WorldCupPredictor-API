using Microsoft.EntityFrameworkCore;
using WorldCupPredictor.API.Data;
using WorldCupPredictor.API.DTOs;
using WorldCupPredictor.API.Models;

namespace WorldCupPredictor.API.Services;

public class BracketService(AppDbContext db) : IBracketService
{
    public async Task<BracketDto?> GetBracketAsync(int userId)
    {
        var bracket = await db.Brackets
            .Include(b => b.GroupPicks).ThenInclude(gp => gp.Group)
            .Include(b => b.Picks).ThenInclude(p => p.Match)
            .Include(b => b.Picks).ThenInclude(p => p.LineupPlayers)
            .Include(b => b.Best3rdPicks)
            .FirstOrDefaultAsync(b => b.UserId == userId);

        return bracket is null ? null : MapToDto(bracket);
    }

    public async Task<BracketDto?> GetBracketByIdAsync(int bracketId)
    {
        var bracket = await db.Brackets
            .Include(b => b.GroupPicks).ThenInclude(gp => gp.Group)
            .Include(b => b.Picks).ThenInclude(p => p.Match)
            .Include(b => b.Picks).ThenInclude(p => p.LineupPlayers)
            .Include(b => b.Best3rdPicks)
            .FirstOrDefaultAsync(b => b.Id == bracketId);

        return bracket is null ? null : MapToDto(bracket);
    }

    public async Task<BracketDto> SaveBracketAsync(int userId, BracketSubmitRequest request)
    {
        var config = await db.TournamentConfigs.FirstOrDefaultAsync(c => c.IsActive);
        if (config != null && DateTime.UtcNow >= config.BracketLockDate)
            throw new InvalidOperationException("Bracket submissions are locked.");

        var bracket = await db.Brackets
            .Include(b => b.GroupPicks)
            .Include(b => b.Picks).ThenInclude(p => p.LineupPlayers)
            .Include(b => b.Best3rdPicks)
            .FirstOrDefaultAsync(b => b.UserId == userId);

        if (bracket is null)
        {
            bracket = new Bracket { UserId = userId, SubmittedAt = DateTime.UtcNow };
            db.Brackets.Add(bracket);
            await db.SaveChangesAsync();
        }
        else
        {
            bracket.SubmittedAt = DateTime.UtcNow;
        }

        // ── Tier ──────────────────────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(request.Tier) &&
            Enum.TryParse<BracketTier>(request.Tier, ignoreCase: true, out var parsedTier))
        {
            bracket.Tier = parsedTier;
        }

        // ── Group picks ───────────────────────────────────────────────────────
        foreach (var gp in request.GroupPicks)
        {
            var existing = bracket.GroupPicks.FirstOrDefault(x => x.GroupId == gp.GroupId);
            if (existing is null)
            {
                bracket.GroupPicks.Add(new BracketGroupPick
                {
                    BracketId = bracket.Id,
                    GroupId = gp.GroupId,
                    FirstTeamId = gp.FirstTeamId,
                    SecondTeamId = gp.SecondTeamId,
                });
            }
            else
            {
                existing.FirstTeamId = gp.FirstTeamId;
                existing.SecondTeamId = gp.SecondTeamId;
            }
        }

        // ── Knockout picks ────────────────────────────────────────────────────
        foreach (var kp in request.KnockoutPicks)
        {
            var existing = bracket.Picks.FirstOrDefault(x => x.MatchId == kp.MatchId);
            if (existing is null)
            {
                var newPick = new BracketPick
                {
                    BracketId = bracket.Id,
                    MatchId = kp.MatchId,
                    PickedTeamId = kp.PickedTeamId,
                    HomeScore = kp.HomeScore,
                    AwayScore = kp.AwayScore,
                };
                bracket.Picks.Add(newPick);
                await db.SaveChangesAsync(); // get newPick.Id

                // Add lineup players
                if (kp.LineupPlayerIds is { Count: > 0 })
                {
                    var ids = kp.LineupPlayerIds.Take(11).Distinct().ToList();
                    foreach (var pid in ids)
                        newPick.LineupPlayers.Add(new BracketPickLineupPlayer { BracketPickId = newPick.Id, PlayerId = pid });
                }
            }
            else
            {
                existing.PickedTeamId = kp.PickedTeamId;
                existing.HomeScore = kp.HomeScore;
                existing.AwayScore = kp.AwayScore;

                // Replace lineup players
                db.BracketPickLineupPlayers.RemoveRange(existing.LineupPlayers);
                existing.LineupPlayers.Clear();

                if (kp.LineupPlayerIds is { Count: > 0 })
                {
                    var ids = kp.LineupPlayerIds.Take(11).Distinct().ToList();
                    foreach (var pid in ids)
                        existing.LineupPlayers.Add(new BracketPickLineupPlayer { BracketPickId = existing.Id, PlayerId = pid });
                }
            }
        }

        // ── Best 3rd place picks (ranks 1-8) ─────────────────────────────────
        foreach (var bp in request.Best3rdPicks)
        {
            var existing = bracket.Best3rdPicks.FirstOrDefault(x => x.Rank == bp.Rank);
            if (existing is null)
            {
                bracket.Best3rdPicks.Add(new BracketBest3rdPick
                {
                    BracketId = bracket.Id,
                    Rank = bp.Rank,
                    TeamId = bp.TeamId,
                });
            }
            else
            {
                existing.TeamId = bp.TeamId;
            }
        }

        await db.SaveChangesAsync();

        // Reload with navigation props for mapping
        var saved = await db.Brackets
            .Include(b => b.GroupPicks).ThenInclude(gp => gp.Group)
            .Include(b => b.Picks).ThenInclude(p => p.Match)
            .Include(b => b.Picks).ThenInclude(p => p.LineupPlayers)
            .Include(b => b.Best3rdPicks)
            .FirstAsync(b => b.Id == bracket.Id);

        return MapToDto(saved);
    }

    private static BracketDto MapToDto(Bracket b) => new(
        b.Id,
        b.IsLocked,
        b.TotalPoints,
        b.SubmittedAt,
        b.Tier.ToString(),
        b.GroupPicks
          .Select(gp => new GroupPickDto(gp.GroupId, gp.Group.Name, gp.FirstTeamId, gp.SecondTeamId))
          .ToList(),
        b.Picks
          .Where(p => p.Match.SlotNumber != null)
          .Select(p => new KnockoutPickDto(
              p.MatchId,
              p.Match.SlotNumber!.Value,
              p.Match.Round.ToString(),
              p.PickedTeamId,
              p.HomeScore,
              p.AwayScore,
              p.LineupPlayers.Select(l => l.PlayerId).ToList()))
          .ToList(),
        b.Best3rdPicks
          .OrderBy(p => p.Rank)
          .Select(p => new Best3rdPickDto(p.Rank, p.TeamId))
          .ToList()
    );
}
